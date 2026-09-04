# Last Seed Survivor — Architecture Audit and Next Roadmap

Audit date: 2026-09-04  
Baseline commit: `fed087b`

## Goal

The game must remain a runnable, testable C# program whose Unity layer supplies input,
time, physics, scene references, rendering, audio, and platform integrations. Unity
components are adapters and views; they do not own rules, application workflows, or
dependency selection.

This roadmap continues the completed foundation without a wholesale rewrite. Each
iteration must leave `master` playable and is committed only after its full gate passes.

## Audit scope and method

The audit covered all production C# files under `Assets/_Project`, assembly dependency
direction, installers, runtime searches, Unity lifecycle methods, events/signals,
pooling, object creation, frame loops, static state, and the automated test inventory.

Generated `Infrastructure/Input/Input Actions.cs` is excluded from size-based refactoring.
Editor-only tools are evaluated separately from runtime code and may legitimately be
larger when they do not leak responsibilities into a runtime type.

Current automated baseline:

- 51 EditMode tests.
- 1 PlayMode direct-Game-scene startup test.
- Zenject validation for all 3 enabled build scenes.

## Audit findings

### P0 — architectural boundaries are documented but not enforced

- `LastSeed.Gameplay` references Infrastructure, Zenject, DOTween, and TextMeshPro and
  contains many MonoBehaviours and presentation classes. A clean C# gameplay core cannot
  currently compile without Unity-facing dependencies.
- All asmdefs have `noEngineReferences: false`; there is no assembly boundary proving
  that domain code is engine-independent.
- Desired dependency direction remains `Core <- Gameplay.Domain <- Application`, with
  Unity/Zenject implementations in adapter assemblies and Bootstrap as the only
  composition layer.

### P0 — hidden composition roots remain

- `RewardInstaller` is a MonoBehaviour that constructs `RewardRollService`,
  `RewardApplyService`, and `RewardFlowController` in `Awake`.
- `AcaciaThornWeapon` constructs and owns its concrete projectile pool.
- Some Unity behaviours accept part of their graph through Zenject and another part
  through ad-hoc `Init`, obscuring required dependencies and valid lifecycle states.

### P0 — weapon implementations duplicate the same application workflow

- `ProjectileWeapon` and `AcaciaThornWeapon` both own cooldown/salvo state, progression
  mutations, runtime-stat publication, damage calculation, firing, and pool dispatch.
- `WeaponRuntimeState` is 411 lines and combines progression storage, limits, mutations,
  derived values, modifier ownership, reset, and numeric clamping.
- Adding a third weapon would require copying orchestration rather than implementing a
  narrow firing/spawn strategy.

### P1 — `RailPath` is a runtime/editor God Object

`RailPath` (590 lines) owns serialized authoring data, legacy waypoint migration,
world-space conversion, corner smoothing, distance-table construction, sampling,
nearest-distance lookup, cache invalidation, editor mutation API, and gizmo drawing.
The sampling algorithm is coupled to Transform and cannot be tested independently.

### P1 — worm orchestration is improved but unfinished

- `WormController` is down to 503 lines but still owns serialized configuration,
  lifecycle reset, Unity update dispatch, rail progress, path completion, section
  rollback coordination, revive entry, and presentation layout creation.
- `WormSegment` still combines pooled entity lifecycle, cocoon state, damage binding,
  renderer/view validation, and per-frame sorting updates.
- `WormSegmentChainPresenter` and `WormSegmentVisualRig` are cohesive presentation code,
  but should be split where calculation can be made engine-independent and tested.

### P1 — reward application flow and UI state are mixed

- `RewardFlowController` owns queueing, session attempts, ad callbacks, rolling,
  application, popup state construction, and direct View event subscriptions.
- `RewardPopupView`, `RewardPopupAnimator`, `RewardButtonView`, and
  `RewardPopupActionControls` contain overlapping binding, animation, interaction, and
  formatting responsibilities.
- Reward selection uses `UnityEngine.Random`, preventing deterministic application tests.

### P1 — events are only partially reactive

- Cross-system notifications correctly use typed SignalBus payloads in many paths.
- Domain events such as `WormSectionHealth.Changed/Destroyed` carry no transition data;
  subscribers must re-read mutable state.
- `WormSection` republishes model events using the mutable section object.
- `RewardPopupView` exposes four separate imperative events rather than one typed user
  intent stream. Local View events are acceptable, but their payloads and ownership must
  be explicit and converted to application commands at one boundary.

### P1 — pooled lifecycle is not yet universal

- Projectiles, worm segments, and damage popups use the shared pool foundation.
- `WormSectionHpPresenter` still instantiates and destroys HP views per lifecycle.
- `AcaciaThornProjectilePool`, `ProjectilePool`, and `WormSegmentPool` share the generic
  base algorithm but still expose different high-level ownership APIs.
- High-level weapon code still knows concrete pool types.

### P2 — UI requires an application-state boundary

- `PopupRoot` combines registry construction, navigation/stack rules, input locking,
  transitions, and reactive requests.
- Revival and victory popup controllers mix application flow with view/platform calls.
- Large animation classes are not automatically problematic, but animation timelines,
  cached visual references, content binding, and user intent must not live in one type.

### P2 — lifecycle and performance hardening

- Runtime `FindObjectOfType`/`GameObject.Find` usage was not found.
- Remaining `GetComponent*` calls are mostly initialization, validation, collision
  boundary, or editor-safe discovery; each must be cached and documented, not removed
  mechanically.
- DOTween owners need a uniform kill/restore contract on disable/destroy and pool return.
- Shared mutable formatter buffers in `RewardTextFormatter` are non-reentrant and should
  become instance-owned or explicitly single-threaded presentation services.
- Static shared material ownership in legendary lightning needs explicit teardown or a
  documented application-lifetime owner.

### P2 — automated coverage is too narrow for current flows

- Pure worm movement and reward policies have useful unit coverage.
- The only PlayMode test primarily validates startup and DI resolution.
- There are no automated flow tests for reward queue/reroll/take-all, revive completion,
  repeated worm spawn/clear, weapon salvo transitions, pooled collision lifecycle,
  popup stack/input locks, or Bootstrap -> Lobby -> Game -> Lobby.

## Target domain map and ownership

```text
Core
  primitives, result types, clock/random contracts, lifecycle contracts

Gameplay.Domain
  worm health/movement state, weapon progression/firing state, rewards policies
  no Unity, Zenject, UI, advertising, scene, Transform, Time, or SignalBus references

Gameplay.Application
  use cases and ordered gameplay stages
  depends on Domain contracts; publishes typed application events

UnityAdapters
  MonoBehaviours, physics/collision, Transform/rail views, ScriptableObject adapters,
  pooled prefab factories, Unity clock/random adapters

Presentation
  ViewModels/presenters, Views, animation drivers, UI intent adapters

Infrastructure
  input, navigation, advertising, persistence, platform SDK implementations

Bootstrap
  ProjectContext/SceneContext installers and bindings only
```

## Required gameplay lifecycle order

One physical Unity update boundary invokes named stages:

```text
CaptureInput
-> AdvanceSession
-> AdvancePlayer
-> AdvanceWeapons
-> AdvanceWorm
-> ResolveGameplayEvents
-> PresentWorld
-> PresentUI
```

Only stages with a real ordering dependency belong in the coordinator. Independent
Unity animation components may retain `Update`/`LateUpdate` when they are pure views.

## Execution roadmap

### Stage 1 — enforce a clean C# seam

1. Add clock and random contracts (`IGameClock`, `IRandomSource`) in Core.
2. Add Unity adapters bound in Bootstrap; preserve current RNG sequence during migration.
3. Create a `LastSeed.Gameplay.Domain` asmdef with `noEngineReferences: true`.
4. Move already-pure models incrementally: movement snapshots/results, section health,
   rollback state, reward selection primitives, and pool membership logic.
5. Keep ScriptableObjects as configuration adapters that build immutable domain settings.

Done when the domain assembly compiles without Unity/Zenject and its tests reference only
that assembly plus NUnit.

### Stage 2 — reward composition and deterministic application flow

1. Replace `RewardInstaller.Awake` construction with `RewardsInstaller` bindings.
2. Split `RewardFlowController` into:
   - `RewardSessionState` — attempt counters/current request;
   - `RewardRequestQueue` — ordered requests;
   - `RewardChoiceRollUseCase` — roll/guarantee orchestration;
   - `RewardAdUseCase` — rewarded action state and result handling;
   - `RewardSelectionUseCase` — apply one/all choices;
   - `RewardFlowPresenter` — maps application state to the ViewModel.
3. Introduce `IRewardPopupView` and immutable `RewardPopupViewModel`.
4. Inject `IRandomSource` into rarity, assist, ad, and weighted selection.
5. Keep advertising optional via explicit no-op/disabled implementation.

Playtest: normal reward, free reroll, failed/successful ad reroll, take-all, queued cocoon
requests, scene restart while ad callback is pending.

### Stage 3 — common weapon runtime and typed spawn boundary

1. Extract `WeaponProgressionState`, `WeaponProgressionLimits`, and derived stat calculator
   from `WeaponRuntimeState`.
2. Create a common `WeaponFireCycle` state machine for cooldown/prepare/salvo transitions.
3. Separate `IShotPatternBuilder`, `IProjectileSpawnSink<TRequest>`, damage calculation,
   and type-specific shot creation.
4. Make both existing weapons thin Unity adapters over the same application lifecycle.
5. Bind both projectile pools in Bootstrap; weapons never construct concrete pools.
6. Publish one immutable weapon-state snapshot after committed mutations.

Playtest: initial fire, delayed animation release, salvo, critical hit, reset, unlock,
both weapon types active, extreme configured limits, and pool exhaustion/rollback.

### Stage 4 — rail domain and adapter split

1. Introduce immutable `RailPathDefinition` authoring data.
2. Extract `RailPathSmoother`, `RailDistanceTableBuilder`, `RailSampler`, and
   `RailNearestPointQuery` as focused calculation classes.
3. Keep `RailPathView` responsible only for Transform conversion and exposing a baked
   runtime path through `IWormRailPath`.
4. Move legacy import and point-editing APIs to Editor assembly utilities.
5. Move gizmo drawing to an editor-only drawer or narrow view component.
6. Cache control-point distances during bake; do not perform repeated nearest-sample
   scans for control-point progress.

Playtest: linear/smoothed paths, short/duplicate points, transformed path object, catch-up,
burst-disable point, revive target, and editor point migration.

### Stage 5 — finish worm application boundary

1. Extract immutable `WormMovementConfig`, `WormPresentationConfig`, and
   `WormReviveConfig` from controller Inspector fields.
2. Introduce `WormLifecycleController` for init/clear and a `WormMovementCoordinator`
   for named forward/rollback/revive states.
3. Replace `WormController.PathCompleted` with a payload containing final distance,
   normalized progress, and completion reason; publish once at the adapter boundary.
4. Split `WormSegment` into pooled entity adapter, damage adapter, cocoon view, and visual
   rig references without repeated component discovery.
5. Extract chain layout calculations from `WormSegmentChainPresenter`; retain Transform
   application in the presenter.
6. Replace HP-view instantiate/destroy with `ObjectPool<WormSectionHpView>` and symmetric
   section-to-view registry cleanup.

Playtest: spawn rollback failure, repeated reset, multiple simultaneous destroyed gaps,
revive during pause, path completion, death, and all cocoon variants.

### Stage 6 — payload-first domain events

1. Define immutable `WormSectionHealthChanged`, `WormSectionDestroyed`,
   `RewardUserIntent`, and weapon progression payloads.
2. Capture old/new values and identity before pooled reset/unbind.
3. Keep local domain events inside aggregate ownership; translate once to scene SignalBus.
4. Remove duplicate publishers and subscriptions only after every consumer is migrated.
5. Add subscription lifecycle tests for enable/disable, reset, and scene unload.

Playtest: damage/reward/death ordering, pooled reuse, scene reload, and duplicate-event
protection.

### Stage 7 — popup state and MVVM

1. Split `PopupRoot` into registry, stack/navigation state, input-lock owner, and view host.
2. Add ViewModels for reward, revive, victory, HUD, lobby, and navigation.
3. Views expose typed user intent and render immutable state only.
4. Split reward button content binding, cached references, and tween factory.
5. Split reward popup timeline orchestration from concrete animation clips.
6. Standardize tween lifecycle (`Play`, `Cancel`, `Restore`) and pool/disable cleanup.

Playtest: rapid clicks, close during transition, nested popup request, timescale zero,
resolution/aspect changes, navigation, and input lock restoration.

### Stage 8 — pooling and identity registries

1. Define a common pooled lifecycle contract and typed `EntityLease<T>`/owner mapping.
2. Migrate remaining HP views and repeated VFX where profiling shows churn.
3. Map damageable entity/view/lifecycle owner in one registry; consumers must not scan
   concrete subtype pools.
4. Verify transactional rent -> initialize -> register -> activate and reverse rollback.
5. Add stress tests for duplicate return, partial initialization failure, scene teardown,
   and stale identity removal.

### Stage 9 — async/platform lifecycle

1. Add cancellation-bound application operations before introducing more async work.
2. Wrap ad callbacks so late completion cannot mutate a disposed scene/session.
3. Add UniTask only where it improves scene loading, popup transitions, prewarming, or
   platform calls; every operation receives an owner lifetime token.
4. Add persistence/analytics as optional interfaces with no-op implementations.

### Stage 10 — test, profiling, and release hardening

1. Add deterministic domain tests for reward distributions and weapon fire cycles.
2. Add PlayMode scenarios for reward, revive, spawn/reset, pooling, and popup stack.
3. Add Bootstrap -> Lobby -> Game -> Lobby integration coverage.
4. Add allocation budgets for gameplay update, weapon fire, worm movement, and popup idle.
5. Profile CPU, GC, pool growth, retained Unity objects, tween counts, and scene unload.
6. Add build-time validation for required references, configs, signal declarations, and
   duplicate stable keys.

## Gate required for every implementation stage

1. Inspect working tree and preserve unrelated user changes.
2. Add/adjust focused EditMode tests for extracted pure logic.
3. Compile Unity scripts without C# errors or new warnings.
4. Validate all enabled build scenes through Zenject.
5. Run the complete EditMode suite.
6. Run the direct-Game PlayMode suite plus the stage-specific scenario.
7. Inspect logs for unexpected exceptions, missing references, and leaked subscriptions.
8. Run `git diff --check` and verify only intended files/assets changed.
9. Commit the stage independently and push only after every gate is green.

Visual, platform-SDK, or timing-sensitive behavior additionally requires a manual Editor
playtest and Profiler check; a DI smoke test alone is not sufficient.

## Explicit non-goals

- Do not split classes solely to reach an arbitrary line count.
- Do not refactor generated Input System code.
- Do not create empty installers, repositories, factories, or interfaces without a
  current second implementation/test seam/ownership reason.
- Do not move all MonoBehaviours out of Gameplay mechanically; move rules first and keep
  thin Unity adapters close to their domain until assembly boundaries are stable.
- Do not introduce ECS, Addressables, UniTask, or a universal event bus before profiling
  or a concrete lifecycle need justifies them.

## Immediate next slice

Start with Stage 1 random/clock seams and Stage 2 reward composition together only as far
as needed to remove `RewardInstaller` as a hidden composition root. This creates a small,
testable vertical slice: deterministic reward domain -> application use case -> popup
adapter -> Zenject composition, without touching unrelated UI animation assets.
