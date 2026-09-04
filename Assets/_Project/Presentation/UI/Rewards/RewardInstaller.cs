using System;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

[DisallowMultipleComponent]
public sealed class RewardInstaller : MonoInstaller
{
    [Header("Refs")]
    [SerializeField] private RewardDatabase _database;
    [SerializeField] private RewardPopupView _popup;
    [SerializeField] private PopupRoot _popupRoot;
    [FormerlySerializedAs("_takeAllRewardedAdService")]
    [SerializeField] private RewardedAdService _rewardedAdService;

    [Header("Session Attempts")]
    [FormerlySerializedAs("_freeRerollAttemptsPerPopup")]
    [SerializeField, Min(0)] private int _freeRerollAttemptsPerSession = 2;
    [FormerlySerializedAs("_adRerollAttemptsPerPopup")]
    [SerializeField, Min(0)] private int _adRerollAttemptsPerSession = 1;
    [FormerlySerializedAs("_takeAllAttemptsPerPopup")]
    [SerializeField, Min(0)] private int _takeAllAttemptsPerSession = 1;

#if UNITY_EDITOR
    public int EditorFreeRerollAttemptsPerSession => _freeRerollAttemptsPerSession;
    public int EditorAdRerollAttemptsPerSession => _adRerollAttemptsPerSession;
    public int EditorTakeAllAttemptsPerSession => _takeAllAttemptsPerSession;
#endif

    public override void InstallBindings()
    {
        ValidateRequiredReferences();

        Container.BindInstance(_database).AsSingle();
        Container.BindInstance(_popup).AsSingle();
        Container.BindInstance(_popupRoot).AsSingle();
        BindRewardedAdService();
        Container.BindInstance(new RewardFlowSettings(
            _freeRerollAttemptsPerSession,
            _adRerollAttemptsPerSession,
            _takeAllAttemptsPerSession));
        Container.Bind<RewardAttemptState>().AsSingle();
        Container.Bind<RewardRequestQueue>().AsSingle();
        Container.Bind<RewardAdOperation>().AsSingle();
        Container.Bind<RewardRollService>().AsSingle();
        Container.Bind<RewardApplyService>().AsSingle();
        Container.BindInterfacesAndSelfTo<RewardFlowController>().AsSingle();
        Container.BindInterfacesAndSelfTo<RewardSessionController>()
            .AsSingle()
            .NonLazy();
    }

    private void BindRewardedAdService()
    {
        if (_rewardedAdService != null)
        {
            Container.Bind<IRewardedAdService>()
                .FromInstance(_rewardedAdService)
                .AsSingle();
            return;
        }

        Container.Bind<IRewardedAdService>()
            .To<DisabledRewardedAdService>()
            .AsSingle();
    }

    private void ValidateRequiredReferences()
    {
        if (_database == null)
            throw new InvalidOperationException("Reward database is not configured.");

        if (_popup == null)
            throw new InvalidOperationException("Reward popup is not configured.");

        if (_popupRoot == null)
            throw new InvalidOperationException("Reward popup root is not configured.");

    }
}
