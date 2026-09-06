using LastSeed.Gameplay.Input;
using LastSeed.Infrastructure.Input;

namespace LastSeed.Bootstrap.GameplayLoop
{
    public sealed class GameplayFrameCoordinator
    {
        private readonly IPlayerInputSnapshotProvider _playerInputSnapshotProvider;
        private readonly IGameplayInputLock _gameplayInputLock;
        private readonly PlayerMovementController _playerMovementController;
        private readonly PlayerWeaponController _playerWeaponController;
        private readonly WormController _wormController;
        private readonly WormCombatBurstSignalPublisher _wormCombatBurstPublisher;
        private readonly WormPathCompletedSignalPublisher _wormPathCompletedPublisher;
        private readonly WormPressureDirector _wormPressureDirector;

        public GameplayFrameCoordinator(
            IPlayerInputSnapshotProvider playerInputSnapshotProvider,
            IGameplayInputLock gameplayInputLock,
            PlayerMovementController playerMovementController,
            PlayerWeaponController playerWeaponController,
            WormController wormController,
            WormCombatBurstSignalPublisher wormCombatBurstPublisher,
            WormPathCompletedSignalPublisher wormPathCompletedPublisher,
            WormPressureDirector wormPressureDirector)
        {
            _playerInputSnapshotProvider = playerInputSnapshotProvider;
            _gameplayInputLock = gameplayInputLock;
            _playerMovementController = playerMovementController;
            _playerWeaponController = playerWeaponController;
            _wormController = wormController;
            _wormCombatBurstPublisher = wormCombatBurstPublisher;
            _wormPathCompletedPublisher = wormPathCompletedPublisher;
            _wormPressureDirector = wormPressureDirector;
        }

        public void Tick(
            float deltaTime,
            float unscaledDeltaTime,
            float time,
            float unscaledTime)
        {
            RunInputCaptureStage();
            RunPlayerStage(deltaTime);
            RunWormStage(deltaTime, unscaledDeltaTime, time, unscaledTime);
            RunDifficultyStage(deltaTime);
        }

        private void RunInputCaptureStage()
        {
            _playerInputSnapshotProvider.CaptureFrame();
        }

        private void RunPlayerStage(float deltaTime)
        {
            if (_gameplayInputLock.IsLocked)
            {
                _playerMovementController.StopMovement();
                return;
            }

            PlayerInputSnapshot inputSnapshot = _playerInputSnapshotProvider.CurrentSnapshot;
            _playerMovementController.Tick(inputSnapshot, deltaTime);
            _playerWeaponController.Tick(deltaTime);
        }

        private void RunWormStage(
            float deltaTime,
            float unscaledDeltaTime,
            float time,
            float unscaledTime)
        {
            WormFrameResult result = _wormController.Tick(
                deltaTime,
                unscaledDeltaTime,
                time,
                unscaledTime);
            _wormCombatBurstPublisher.PublishIfChanged(
                _wormController.IsCombatBurstActive);
            _wormPathCompletedPublisher.Publish(result);
        }

        private void RunDifficultyStage(float deltaTime)
        {
            _wormPressureDirector.Tick(deltaTime);
        }
    }
}
