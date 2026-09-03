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

        public GameplayFrameCoordinator(
            IPlayerInputSnapshotProvider playerInputSnapshotProvider,
            IGameplayInputLock gameplayInputLock,
            PlayerMovementController playerMovementController,
            PlayerWeaponController playerWeaponController)
        {
            _playerInputSnapshotProvider = playerInputSnapshotProvider;
            _gameplayInputLock = gameplayInputLock;
            _playerMovementController = playerMovementController;
            _playerWeaponController = playerWeaponController;
        }

        public void Tick(float deltaTime)
        {
            _playerInputSnapshotProvider.CaptureFrame();

            if (_gameplayInputLock.IsLocked)
            {
                _playerMovementController.StopMovement();
                return;
            }

            PlayerInputSnapshot inputSnapshot = _playerInputSnapshotProvider.CurrentSnapshot;
            _playerMovementController.Tick(inputSnapshot, deltaTime);
            _playerWeaponController.Tick(deltaTime);
        }
    }
}
