using System.Collections.Generic;
using LastSeed.Gameplay.Signals;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

[DisallowMultipleComponent]
public sealed class RewardInstaller : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RewardDatabase _database;

    [SerializeField] private RewardPopupView _popup;
    [SerializeField] private PopupRoot _popupRoot;
    [SerializeField] private ProjectileWeapon _weapon;
    [SerializeField] private AcaciaThornWeapon _acaciaThornWeapon;
    [FormerlySerializedAs("_takeAllRewardedAdService")]
    [SerializeField] private RewardedAdService _rewardedAdService;

    [Header("Session Attempts")]
    [FormerlySerializedAs("_freeRerollAttemptsPerPopup")]
    [SerializeField][Min(0)] private int _freeRerollAttemptsPerSession = 2;
    [FormerlySerializedAs("_adRerollAttemptsPerPopup")]
    [SerializeField][Min(0)] private int _adRerollAttemptsPerSession = 1;
    [FormerlySerializedAs("_takeAllAttemptsPerPopup")]
    [SerializeField][Min(0)] private int _takeAllAttemptsPerSession = 1;

    private RewardFlowController _rewardFlow;
    private bool _hasRevivedThisRun;
    private SignalBus _signalBus;
    private IRandomSource _randomSource;
    private bool _isSubscribedToSignals;

    [Inject]
    public void Construct(SignalBus signalBus, IRandomSource randomSource)
    {
        _signalBus = signalBus;
        _randomSource = randomSource;
        SubscribeToSignals();
    }

    public IReadOnlyList<CocoonRewardProfile> CocoonProfiles =>
        _database != null
            ? _database.CocoonProfiles
            : CocoonRewardProfile.Defaults;

#if UNITY_EDITOR
    public int EditorFreeRerollAttemptsPerSession => _freeRerollAttemptsPerSession;
    public int EditorAdRerollAttemptsPerSession => _adRerollAttemptsPerSession;
    public int EditorTakeAllAttemptsPerSession => _takeAllAttemptsPerSession;
#endif

    private void Awake()
    {
        var roll = new RewardRollService(_database, _randomSource);
        var apply = new RewardApplyService(_weapon, _acaciaThornWeapon);

        _rewardFlow = new RewardFlowController(
            roll,
            apply,
            _popup,
            _popupRoot,
            _rewardedAdService,
            _randomSource,
            _freeRerollAttemptsPerSession,
            _adRerollAttemptsPerSession,
            _takeAllAttemptsPerSession);
    }

    private void OnEnable()
    {
        SubscribeToSignals();
    }

    private void OnDisable()
    {
        UnsubscribeFromSignals();
    }

    private void OnDestroy()
    {
        _rewardFlow?.Dispose();
    }

    public bool OpenReward()
    {
        return OpenReward(null);
    }

    public bool OpenReward(CocoonRewardProfile cocoonProfile)
    {
        return OpenReward(cocoonProfile, 0f, 0f);
    }

    public bool OpenReward(
        CocoonRewardProfile cocoonProfile,
        float headPathProgressNormalized,
        float wormDestructionProgressNormalized)
    {
        return _rewardFlow != null &&
            _rewardFlow.Open(
                cocoonProfile,
                new RewardRollContext(
                    headPathProgressNormalized,
                    wormDestructionProgressNormalized,
                    _hasRevivedThisRun));
    }

    public void ResetSession()
    {
        _hasRevivedThisRun = false;
        _rewardFlow?.ResetSession();
    }

    private void HandleReviveGranted(WormReviveGrantedSignal signal)
    {
        _hasRevivedThisRun = true;
    }

    private void HandleRewardRequested(WormRewardRequestedSignal signal)
    {
        OpenReward(
            signal.RewardProfile,
            signal.HeadPathProgressNormalized,
            signal.WormDestructionProgressNormalized);
    }

    private void SubscribeToSignals()
    {
        if (_signalBus == null || _isSubscribedToSignals || !isActiveAndEnabled)
            return;

        _signalBus.Subscribe<WormReviveGrantedSignal>(HandleReviveGranted);
        _signalBus.Subscribe<WormRewardRequestedSignal>(HandleRewardRequested);
        _isSubscribedToSignals = true;
    }

    private void UnsubscribeFromSignals()
    {
        if (_signalBus == null || !_isSubscribedToSignals)
            return;

        _signalBus.Unsubscribe<WormReviveGrantedSignal>(HandleReviveGranted);
        _signalBus.Unsubscribe<WormRewardRequestedSignal>(HandleRewardRequested);
        _isSubscribedToSignals = false;
    }
}
