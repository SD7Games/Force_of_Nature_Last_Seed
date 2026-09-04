using LastSeed.Infrastructure.Input;
using UnityEngine;
using Zenject;

namespace _Project.App.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayRunRestarter : MonoBehaviour
    {
        [Header("Worm")]
        [SerializeField] private WormSpawner _wormSpawner;
        [SerializeField] private WormPressureDirector _pressureDirector;
        [SerializeField] private WormEngagementController _engagementController;
        [SerializeField] private WormReviveFlowController _reviveFlowController;

        [Header("Rewards/UI")]
        [SerializeField] private PopupRoot _popupRoot;
        [SerializeField] private WormDamagePopupPresenter _damagePopupPresenter;

        private bool _isRestarting;
        private IPlayerInputSnapshotProvider _playerInputSnapshotProvider;
        private PlayerMovementController _playerMovementController;
        private PlayerWeaponController _playerWeaponController;
        private RewardSessionController _rewardSessionController;

        [Inject]
        public void Construct(
            IPlayerInputSnapshotProvider playerInputSnapshotProvider,
            PlayerMovementController playerMovementController,
            PlayerWeaponController playerWeaponController,
            RewardSessionController rewardSessionController)
        {
            _playerInputSnapshotProvider = playerInputSnapshotProvider;
            _playerMovementController = playerMovementController;
            _playerWeaponController = playerWeaponController;
            _rewardSessionController = rewardSessionController;
        }

        public void RestartRun()
        {
            if (_isRestarting)
                return;

            _isRestarting = true;

            _popupRoot?.HideActive();
            _popupRoot?.ReleaseGameplayLock();
            Time.timeScale = 1f;

            _playerInputSnapshotProvider.ResetState();
            _playerMovementController.ResetForNewRun();
            _engagementController?.ResetState();
            _pressureDirector?.ResetForNewRun();
            _reviveFlowController?.ResetForNewRun();

            _playerWeaponController.ClearTransientState();
            _damagePopupPresenter?.ClearActivePopups();

            _wormSpawner?.DespawnWorm();

            _playerWeaponController.ResetRuntimeState();
            _rewardSessionController.ResetSession();

            _wormSpawner?.SpawnWorm();

            _isRestarting = false;
        }
    }
}
