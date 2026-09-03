using LastSeed.Gameplay.Input;
using LastSeed.Infrastructure.Input;

namespace LastSeed.Bootstrap.GameplayLoop
{
    public sealed class GameplayFrameCoordinator
    {
        private readonly IPlayerInputSnapshotProvider _playerInputSnapshotProvider;
        private readonly IGameplayInputLock _gameplayInputLock;
        private readonly PlayerController _playerController;
        private readonly PlayerShooter _playerShooter;

        public GameplayFrameCoordinator(
            IPlayerInputSnapshotProvider playerInputSnapshotProvider,
            IGameplayInputLock gameplayInputLock,
            PlayerController playerController,
            PlayerShooter playerShooter)
        {
            _playerInputSnapshotProvider = playerInputSnapshotProvider;
            _gameplayInputLock = gameplayInputLock;
            _playerController = playerController;
            _playerShooter = playerShooter;
        }

        public void Tick(float deltaTime)
        {
            _playerInputSnapshotProvider.CaptureFrame();

            if (_gameplayInputLock.IsLocked)
            {
                _playerController.StopMovement();
                return;
            }

            PlayerInputSnapshot inputSnapshot = _playerInputSnapshotProvider.CurrentSnapshot;
            _playerController.Tick(inputSnapshot, deltaTime);
            _playerShooter.Tick();
        }
    }
}
