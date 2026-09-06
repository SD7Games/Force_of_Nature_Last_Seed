using System;
using Cysharp.Threading.Tasks;
using LastSeed.Gameplay.Signals;
using LastSeed.Infrastructure.Navigation;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

[DisallowMultipleComponent]
public sealed class WormReviveFlowController : MonoBehaviour
{
    [SerializeField] private WormController _wormController;
    [SerializeField] private WormCombatController _wormCombat;
    [SerializeField] private PoolRegistry _projectilePoolRegistry;
    [SerializeField] private ProjectileWeapon _projectileWeapon;
    [SerializeField] private AcaciaThornWeapon _acaciaThornWeapon;
    [SerializeField] private WormDamagePopupPresenter _damagePopupPresenter;
    [SerializeField] private PopupRoot _popupRoot;
    [SerializeField] private RevivalPopupView _revivalPopup;
    [SerializeField] private RewardedAdService _rewardedAdService;
    [SerializeField, Min(0)] private int _maxReviveAttempts = 1;
    [SerializeField, Range(0f, 1f)] private float _fallbackRemainingLevelNormalized = 0.5f;
    [FormerlySerializedAs("_reloadCurrentSceneOnGiveUp")]
    [SerializeField] private bool _returnToLobbyOnGiveUp = true;
    [FormerlySerializedAs("_giveUpRestartAnimationDuration")]
    [SerializeField, Min(0f)] private float _popupCloseAnimationDuration = 0.55f;
    [FormerlySerializedAs("_giveUpRestartTargetScale")]
    [SerializeField, Range(0.5f, 1f)] private float _popupCloseAnimationTargetScale = 0.92f;

    private int _remainingRevives;
    private bool _isFailState;
    private bool _isReviving;
    private bool _isRevivePopupClosePending;
    private bool _isReviveRollbackPending;
    private ISceneNavigator<GameSceneId> _sceneNavigator;
    private SignalBus _signalBus;
    private bool _isSubscribedToSignals;

    [Inject]
    public void Construct(ISceneNavigator<GameSceneId> sceneNavigator, SignalBus signalBus)
    {
        _sceneNavigator = sceneNavigator;
        _signalBus = signalBus;
        SubscribeToSignals();
    }

#if UNITY_EDITOR
    public int EditorMaxReviveAttempts => _maxReviveAttempts;
#endif

    private void Awake()
    {
        _remainingRevives = _maxReviveAttempts;
    }

    private void OnEnable()
    {
        SubscribeToSignals();

        if (_revivalPopup != null)
        {
            _revivalPopup.ReviveRequested += HandleReviveRequested;
            _revivalPopup.GiveUpRequested += HandleGiveUpRequested;
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromSignals();

        if (_revivalPopup != null)
        {
            _revivalPopup.ReviveRequested -= HandleReviveRequested;
            _revivalPopup.GiveUpRequested -= HandleGiveUpRequested;
        }

        _popupRoot?.ReleaseGameplayLock();
        _isRevivePopupClosePending = false;
        _isReviveRollbackPending = false;
    }

    private void HandlePathCompleted(WormPathCompletedSignal signal)
    {
        if (_isFailState || _isReviving)
            return;

        ClearTransientGameplay();
        _isFailState = true;
        ShowRevivalPopup();
    }

    private void SubscribeToSignals()
    {
        if (_signalBus == null || _isSubscribedToSignals || !isActiveAndEnabled)
            return;

        _signalBus.Subscribe<WormPathCompletedSignal>(HandlePathCompleted);
        _isSubscribedToSignals = true;
    }

    private void UnsubscribeFromSignals()
    {
        if (_signalBus == null || !_isSubscribedToSignals)
            return;

        _signalBus.Unsubscribe<WormPathCompletedSignal>(HandlePathCompleted);
        _isSubscribedToSignals = false;
    }

    private void ShowRevivalPopup()
    {
        if (_popupRoot == null || _revivalPopup == null)
        {
            Debug.LogError("WormReviveFlowController: popup references are missing.", this);
            return;
        }

        _revivalPopup.Bind(
            _remainingRevives,
            GetCurrentLevelProgressNormalized(),
            GetCurrentRemainingLevelNormalized(),
            _remainingRevives > 0);

        _popupRoot.Show(_revivalPopup);
    }

    private void HandleReviveRequested()
    {
        if (!_isFailState || _isReviving || _remainingRevives <= 0)
            return;

        _revivalPopup.SetWaitingForAd(true);

        if (_rewardedAdService == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("WormReviveFlowController: rewarded ad service is missing. Granting revive in editor/development build.", this);
            CompleteRewardedAd(true);
#else
            Debug.LogError("WormReviveFlowController: rewarded ad service is missing.", this);
            _revivalPopup.SetWaitingForAd(false);
#endif
            return;
        }

        if (!_rewardedAdService.IsReady)
        {
            Debug.LogWarning("WormReviveFlowController: rewarded ad is not ready.", this);
            _revivalPopup.SetWaitingForAd(false);
            return;
        }

        _rewardedAdService.ShowRewardedAd(CompleteRewardedAd);
    }

    private void CompleteRewardedAd(bool rewardGranted)
    {
        if (!rewardGranted)
        {
            _revivalPopup.SetWaitingForAd(false);
            return;
        }

        _signalBus.Fire<WormReviveGrantedSignal>();
        StartReviveRollbackWithPopupClose();
    }

    private void StartReviveRollbackWithPopupClose()
    {
        _isRevivePopupClosePending = true;
        _isReviveRollbackPending = true;

        StartReviveRollback();
        PlayPopupCloseAnimation(CompleteRevivePopupClose);
    }

    private void StartReviveRollback()
    {
        if (_wormController == null)
        {
            Debug.LogError("WormReviveFlowController: worm controller is missing.", this);
            CompleteReviveRollback();
            return;
        }

        _remainingRevives = Mathf.Max(0, _remainingRevives - 1);
        _isReviving = true;

        if (!_wormController.RollbackToReviveStart(CompleteReviveRollback))
            CompleteReviveRollback();
    }

    private void CompleteReviveRollback()
    {
        _isFailState = false;
        _isReviving = false;
        _isReviveRollbackPending = false;

        _signalBus.Fire<WormReviveRollbackCompletedSignal>();
        ReleaseGameplayLockWhenReviveVisualsComplete();
    }

    private void CompleteRevivePopupClose()
    {
        _popupRoot?.HideActive(releaseGameplayLock: false);
        _isRevivePopupClosePending = false;

        ReleaseGameplayLockWhenReviveVisualsComplete();
    }

    private void HandleGiveUpRequested()
    {
        if (!_returnToLobbyOnGiveUp)
        {
            _popupRoot?.HideActive();
            return;
        }

        PlayPopupCloseAnimation(RequestLobbyLoad);
    }

    public void ResetForNewRun()
    {
        _remainingRevives = _maxReviveAttempts;
        _isFailState = false;
        _isReviving = false;
        _isRevivePopupClosePending = false;
        _isReviveRollbackPending = false;
    }

    private void RequestLobbyLoad()
    {
        _popupRoot?.HideActive();
        NavigateToLobbyAsync().Forget();
    }

    private async UniTask NavigateToLobbyAsync()
    {
        try
        {
            await _sceneNavigator.TryNavigateAsync(
                GameSceneId.Lobby,
                destroyCancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private void PlayPopupCloseAnimation(System.Action onComplete)
    {
        if (_revivalPopup == null)
        {
            onComplete?.Invoke();
            return;
        }

        _revivalPopup.SetWaitingForAd(true);
        _revivalPopup.PlayCloseAnimation(
            _popupCloseAnimationDuration,
            _popupCloseAnimationTargetScale,
            onComplete);
    }

    private void ReleaseGameplayLockWhenReviveVisualsComplete()
    {
        if (_isRevivePopupClosePending || _isReviveRollbackPending)
            return;

        _popupRoot?.ReleaseGameplayLock();
    }

    private float GetCurrentRemainingLevelNormalized()
    {
        if (_wormCombat == null || _wormCombat.TotalProgressSegments <= 0)
            return _fallbackRemainingLevelNormalized;

        return _wormCombat.RemainingProgressNormalized;
    }

    private float GetCurrentLevelProgressNormalized()
    {
        if (_wormCombat == null || _wormCombat.TotalProgressSegments <= 0)
            return 1f - _fallbackRemainingLevelNormalized;

        return _wormCombat.DestructionProgressNormalized;
    }

    private void ClearTransientGameplay()
    {
        _projectilePoolRegistry?.ReleaseAllActiveProjectiles();
        _projectileWeapon?.ClearTransientState();
        _acaciaThornWeapon?.ClearTransientState();
        _damagePopupPresenter?.ClearActivePopups();
    }
}
