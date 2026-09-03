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

- Install SignalBus in Game SceneContext.
- Replace `WormRewardEvents`, `WormReviveEvents`, `GameplayRunRestartEvents`, `PopupEvents`, and static combat events.
- Replace static `CombatState` with a scene-scoped session model.
- Use direct interfaces when a caller requires a return value; use signals for one-to-many notifications.

### 4. Player and weapons

- Separate player input/application logic from the player Unity view.
- Extract weapon runtime models and command services.
- Remove cross-domain serialized references from player and weapon behaviours.
- Preserve ScriptableObject balance configuration.

### 5. Unified pooling

- Introduce one generic `ObjectPool<T>` algorithm and stable pooled lifecycle contracts.
- Migrate projectiles, worm segments, VFX, damage popups, and audio sources incrementally.
- Centralize rent -> initialize -> bind -> activate -> rollback-on-failure.
- Remove subtype probing and duplicated pool orchestration.

### 6. Worm domain

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
