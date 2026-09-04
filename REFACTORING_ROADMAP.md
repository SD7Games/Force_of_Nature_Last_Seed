# Last Seed Survivor — Architecture Refactoring Roadmap

## Non-negotiable rules

- Every iteration must compile and preserve a playable runtime path.
- Composition roots contain bindings only: no `Resolve`, service locator, gameplay logic, or hidden runtime strategy selection.
- Project-wide and scene-wide lifetimes are separated through `ProjectContext` and `SceneContext`.
- Pure C# classes use constructor injection. `MonoBehaviour` uses method injection only at the Unity boundary.
- Inspector references remain only for scene ownership, prefab configuration, and visual assets.
- UI is migrated to MVVM. World presentation is separated only where rules/state and Unity presentation are mixed.
- Signals are scene-scoped by default. Project-scoped signals require an explicit cross-scene use case.
- All subscriptions, async operations, tweens, addressable handles, and pooled registrations have symmetric cleanup.
- Magic technical values become named constants; balance values remain in ScriptableObject configurations.
- No runtime `Find*`, repeated hot-path `GetComponent`, LINQ, closures, or avoidable allocations in frame loops.

## Target dependency direction

```text
Core <- Infrastructure <- Gameplay <- Presentation <- Bootstrap
```

Bootstrap may reference every layer. Lower layers must never reference Bootstrap or Presentation.

## Installer ownership

All installers live in `Bootstrap/Installers` because they are composition roots that may reference several architectural layers. They are grouped by lifetime and scene (`Project`, `Game`, and later `Lobby`) rather than placed inside the implementation layer they bind.

### ProjectContext

- `ApplicationBootstrapInstaller`
- `SceneNavigationInstaller`
- `PerformanceInstaller`
- `AdvertisingInstaller`
- `PersistenceInstaller`

### Game SceneContext

- `GameSignalsInstaller`
- `GameWorldInstaller`
- `GameplayInputInstaller`
- `PlayerInstaller`
- `WormInstaller`
- `CombatInstaller`
- `WeaponsInstaller`
- `ProjectilesInstaller`
- `PoolingInstaller`
- `RewardsInstaller`
- `ReviveInstaller`
- `GameSessionInstaller`
- `GameUiInstaller`

### Lobby SceneContext

- `LobbyUiInstaller`

Installers are introduced only when their domain is migrated. Empty speculative installers are not created.

## Iterations

### 1. Zenject foundation and composition roots

**Status: completed and verified in Unity Play Mode.**

- [x] Add ProjectContext and SceneContext assets.
- [x] Replace hand-written application singleton/bootstrap creation.
- [x] Replace static scene navigation with `ISceneNavigationService`.
- [x] Replace `GameSceneBootstrap` with `GameWorldInstaller` and `GameWorldInitializer`.
- [x] Add automatic dependency validation before Play Mode and build.
- [x] Validate all enabled build scenes in Unity batch mode.
- [x] Run Bootstrap -> Lobby -> Game -> Lobby in Play Mode.
- [x] Run Game directly in Play Mode.

### 2. Input and game loop

**Status: completed and verified by automated EditMode and PlayMode tests.**

- [x] Add immutable `PlayerInputSnapshot`.
- [x] Poll physical input once per frame.
- [x] Replace static `GameplayInputBlocker` with scoped `IGameplayInputLock`.
- [x] Introduce an explicit named gameplay update coordinator only where ordering is required.
- [x] Split the Game SceneContext bindings into focused world, input, player, projectile-pool, and game-loop installers.
- [x] Add a permanent direct-Game-scene PlayMode startup smoke test.

### 3. Signals and session state

**Status: completed and verified by automated EditMode and PlayMode tests.**

- [x] Install SignalBus in Game SceneContext through a focused `GameSignalsInstaller`.
- [x] Replace static worm reward, revive, death, popup, and combat notifications with declared scene-scoped signals.
- [x] Remove the unused static gameplay restart event bus while preserving the explicit restart command API.
- [x] Replace static `CombatState` with scene-scoped `ICombatSessionState` and `CombatSessionState`.
- [x] Add `GameSessionInstaller` so mutable run state is owned by the Game SceneContext.
- [x] Verify SignalBus delivery and session-state resolution in the direct-Game PlayMode smoke test.

### 4. Player and weapons

**Status: completed and verified by automated EditMode and PlayMode tests.**

- [x] Separate player input/application logic from the player Unity view through a pure runtime movement model and controller.
- [x] Extract player weapon initialization, ticking, cleanup, and reset commands into a constructor-injected service.
- [x] Remove the obsolete `PlayerController` and `PlayerShooter` scene behaviours and their cross-domain serialized references.
- [x] Pass frame time explicitly from the named gameplay loop into weapon runtime ticking.
- [x] Preserve scene-owned movement tuning and ScriptableObject weapon balance configuration.

### 5. Unified pooling

**Status: completed and verified by automated EditMode and PlayMode tests.**

- [x] Introduce one generic `ObjectPool<T>` algorithm with active membership, duplicate-return protection, bulk return, and transactional rent rollback.
- [x] Migrate main projectiles, Acacia Thorn projectiles, worm segments, and damage popups to the shared algorithm.
- [x] Centralize projectile rent -> initialize -> activate -> rollback-on-failure in their owning pools.
- [x] Keep type-specific creation and reset hooks in narrow adapters; no pooled audio or standalone VFX owners currently exist to migrate.
- [x] Centralize typed-state rent/initialize/rollback so projectile adapters no longer duplicate transaction handling or allocate captured initialization closures.
- [x] Track active pool indices with `Dictionary<T, int>` and swap-remove for O(1) returns while retaining a compact list for bulk return.

### 6. Worm domain

- In progress: worm segment creation is transactional and rolls every rented segment back on partial failure; damage receivers are cached once per pooled segment and rebound on reuse.
- Adaptive HP orchestration is extracted from `WormSpawner` into a constructor-injected C# service with explicit policy, weapon-power, path-progress, and settings dependencies; `WormInstaller` owns its scene composition.
- Adaptive HP policy resolution has EditMode coverage, and the migrated graph is covered by all-scene Zenject validation plus the direct-Game PlayMode smoke test.
- Section HP, damage, and destruction transitions live in the engine-independent `WormSectionHealth` model; adaptive balancing targets a narrow C# contract rather than a Unity segment implementation.
- Combat-burst timing, speed transitions, and state changes are isolated in a Zenject-created `WormCombatBurstController`; `WormController` now supplies only rail-derived facts and applies the result to Unity movement.
- Catch-up, revive, and burst-disable target lookup/caching are isolated behind `IWormRailPath`; the resolver is engine-independent and `RailPath` is its Unity adapter.
- Per-frame segment visibility, rail sampling, transform rotation, wave offsets, and head/tail visual-chain layout are owned by the injected `WormSegmentChainPresenter`, leaving `WormController` as lifecycle/movement coordination rather than presentation implementation.
- Revive trajectory/easing calculations are engine-independent in `WormReviveMotionCalculator`; cached Transform scale capture/application is isolated in `WormReviveVisualScaler`, and both are composed through Zenject.
- Section rollback targets and anchored-tail lifecycle are owned by the generic engine-independent `WormSectionRollbackState<TSegment>`; repeated destroyed-segment lookup reuses a `HashSet` and no longer allocates a closure for `List.RemoveAll`.
- Worm spawn/pool settings, `WormSegmentPool`, and `WormFactory` are composed by `WormInstaller`; `WormSpawner` no longer acts as a hidden composition root or constructs gameplay collaborators in `Awake`.
- Combat-burst changes are published as immutable scene-scoped signals by a lifecycle-managed adapter; the face presenter reacts through SignalBus and no longer couples `WormSpawner` directly to controller events or runtime component searches.
- Damage-view requests, destruction progress, and path completion are immutable SignalBus payloads; popup/progress/revive subscribers react without direct event subscriptions to worm controllers, and the unused section-damaged event was removed.
- Synchronized cocoon shake is owned by a scene-scoped Zenject service and explicitly supplied to pooled segment views; `WormSegment` no longer owns static tween state that can leak across scene lifecycles.
- Both weapon implementations publish the same typed runtime-stats signal with source and timestamp; adaptive worm HP no longer subscribes to concrete weapon classes. The attack animation also reacts to a cooldown payload through SignalBus.
- Segment renderer discovery, relative sorting, and head/tail visual-chain pose operations are isolated in `WormSegmentVisualRig`; the pooled `WormSegment` keeps only entity lifecycle, damage binding, and cocoon state.
- Worm creation is an explicit `WormSpawnLifecycle` pipeline (`views -> section models -> gameplay/presentation bind -> commit`) with reverse cleanup and pool return on failure; `WormSpawner` is now only the Unity timing and reactive-event adapter.
- Reward slot rarity generation, guaranteed-slot weighting, and secondary legendary promotion are isolated in `RewardRarityRoller`, preserving the existing RNG call order while reducing `RewardRollService` responsibilities.
- Weapon DPS imbalance estimation and its immutable weighting policy are isolated in `RewardWeaponDpsBiasCalculator`/`RewardWeaponDpsBias`; reward selection consumes the result without owning weapon-power formulas.
- Reward eligibility, category/weapon classification, assist-DPS preference, and effective weight calculation are centralized in `RewardSelectionPolicy` so all selection paths share one rule implementation.
- Reward pool construction/filtering, pool inspection, weighted removal, and rarity/fallback selection scenarios are isolated in `RewardPoolBuilder`, `RewardPoolInspector`, `RewardWeightedPicker`, and `RewardChoiceSelector`; `RewardRollService` is reduced from 806 lines to a 253-line orchestration boundary without changing RNG traversal order.
- Worm head advancement is a pure `WormForwardMotionController` step returning distance, catch-up state, and path-completion data; `WormController` applies the result instead of owning speed/burst/target calculations.
- Worm chain ownership, source copying, duplicate-safe destroyed-segment membership, removal, and first-gap lookup are isolated in the engine-independent `WormSegmentChain<TSegment>`; presentation and revive scaling consume read-only chain views.
- Revive squash, throw, deceleration, landing, and phase completion are an explicit tick-driven `WormReviveAnimationController` state machine; `WormController` only applies its immutable frame result to the chain view and owns the Unity callback boundary.
- Split generation, movement, combat, adaptive HP, lifecycle, and world presentation.
- Replace direct reward/revive knowledge with contracts and signals.
- Introduce registries/maps for identity lookup and symmetric unregister.

### 7. Rewards and revive

- Replace the current `RewardInstaller` MonoBehaviour with bindings plus a reward flow service.
- Separate reward state, ViewModel, popup View, roll policy, and application service.
- Model revive as an explicit state machine with cancellation-safe transitions.

### 8. UI MVVM

- Add ViewModels for lobby, HUD, reward popup, revive popup, victory popup, and navigation.
- Views contain references, rendering, and user-input forwarding only.
- ViewModels contain state and presentation decisions; gameplay rules remain in Gameplay.

### 9. Async lifecycle

- Add UniTask after DI lifetimes are stable.
- Migrate scene loading, ads, popup transitions, prewarming, and delayed gameplay flows.
- Every async owner exposes or receives a `CancellationToken` tied to its lifetime.
- Remove coroutines only where UniTask materially improves composition or cancellation.

### 10. Addressables decision and integration

- Profile build size, memory residency, scene dependencies, and load stalls first.
- Use Addressables for remote, large, or optional content, not as a replacement for ordinary serialized references.
- Centralize handles and guarantee release on scene or session teardown.

### 11. Tests, profiling, and hardening

- Unit-test pure models, policies, state machines, formatters, and pool lifecycle.
- Add integration tests for installers and critical scene flows.
- Validate scenes through Zenject before play and build.
- Profile allocations, object counts, async cancellation, scene transitions, and pool retention.

## Definition of done for every iteration

- Unity script compilation succeeds with no C# errors or warnings.
- Relevant Zenject object graphs validate.
- Automated PlayMode smoke tests cover the changed runtime path and succeed.
- Manual Play Mode verification is requested only for visual, interactive, platform-specific, or otherwise non-automatable behavior.
- No new missing scripts or serialized references.
- New subscriptions and resources have symmetric cleanup.
- Git diff contains only the intended iteration.
- Roadmap is updated with completed work and remaining risks.
