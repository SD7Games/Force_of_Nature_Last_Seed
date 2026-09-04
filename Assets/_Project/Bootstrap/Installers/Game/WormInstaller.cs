using UnityEngine;
using Zenject;

namespace LastSeed.Bootstrap.Installers
{
    public sealed class WormInstaller : MonoInstaller
    {
        [Header("Scene Components")]
        [SerializeField] private WormSpawner _wormSpawner;
        [SerializeField] private WormController _wormController;
        [SerializeField] private WormCombatController _wormCombatController;
        [SerializeField] private WormSectionHpPresenter _sectionHpPresenter;
        [SerializeField] private WormPressureDirector _wormPressureDirector;

        [Header("Segment Pool")]
        [SerializeField] private WormSegment _headPrefab;
        [SerializeField] private WormSegment _bodyPrefab;
        [SerializeField] private WormSegment _tailPrefab;
        [SerializeField, Min(1)] private int _sectionCount = 9;
        [SerializeField, Min(0)] private int _poolPadding = 10;
        [SerializeField, Min(1)] private int _prewarmBatchSize = 64;

        [Header("Adaptive HP")]
        [SerializeField] private WormHpScalingConfig _hpScalingConfig;
        [SerializeField, Min(1)] private int _levelNumber = 1;
        [SerializeField, Min(1)] private int _upgradeRebalanceInterval = 1;
        [SerializeField, Min(0f)] private float _minimumRebalanceInterval = 5f;

#if UNITY_EDITOR
        public WormHpScalingConfig EditorHpScalingConfig => _hpScalingConfig;
        public int EditorLevelNumber => _levelNumber;
        public int EditorSectionCount => _sectionCount;
#endif

        public override void InstallBindings()
        {
            Container.Bind<WormSpawner>().FromInstance(_wormSpawner).AsSingle();
            Container.Bind<WormController>().FromInstance(_wormController).AsSingle();
            Container.Bind<WormCombatController>().FromInstance(_wormCombatController).AsSingle();
            Container.Bind<WormSectionHpPresenter>().FromInstance(_sectionHpPresenter).AsSingle();
            Container.Bind<WormPressureDirector>().FromInstance(_wormPressureDirector).AsSingle();
            Container.BindInstance(new WormSpawnSettings(
                _sectionCount,
                _poolPadding,
                _prewarmBatchSize));
            Container.BindInstance(new WormSegmentPoolSettings(
                _wormSpawner.transform,
                _headPrefab,
                _bodyPrefab,
                _tailPrefab));
            Container.BindInterfacesAndSelfTo<WormCocoonShakeClock>().AsSingle();
            Container.Bind<WormSegmentPool>().AsSingle();
            Container.Bind<WormFactory>().AsSingle();
            Container.Bind<WormSpawnLifecycle>().AsSingle();
            Container.Bind<IWormPathProgressProvider>().FromInstance(_wormController).AsSingle();
            Container.Bind<IWormHpScalingPolicy>().FromInstance(_hpScalingConfig).AsSingle();
            Container.Bind<IWeaponPowerProvider>().To<WeaponPowerProvider>().AsSingle();
            Container.Bind<WormCombatBurstController>().AsSingle();
            Container.Bind<WormForwardMotionController>().AsSingle();
            Container.Bind<WormPathProgressState>().AsSingle();
            Container.BindInterfacesAndSelfTo<WormCombatBurstSignalPublisher>()
                .AsSingle()
                .NonLazy();
            Container.BindInterfacesAndSelfTo<WormPathCompletedSignalPublisher>()
                .AsSingle()
                .NonLazy();
            Container.Bind<WormRailTargetResolver>().AsSingle();
            Container.Bind<WormSegmentChainPresenter>().AsSingle();
            Container.Bind<WormReviveMotionCalculator>().AsSingle();
            Container.Bind<WormReviveAnimationController>().AsSingle();
            Container.Bind<WormReviveVisualScaler>().AsSingle();
            Container.Bind<WormReviveSequence>().AsSingle();
            Container.Bind<WormSegmentChain<WormSegment>>().AsSingle();
            Container.BindInterfacesAndSelfTo<WormFaceBurstPresenter>()
                .AsSingle()
                .NonLazy();
            Container.Bind<WormSectionRollbackState<WormSegment>>().AsSingle();
            Container.Bind<WormSectionRollbackMotionController<WormSegment>>().AsSingle();
            Container.BindInstance(new WormAdaptiveHpSettings(
                _levelNumber,
                _upgradeRebalanceInterval,
                _minimumRebalanceInterval));
            Container.Bind<WormAdaptiveHpController>().AsSingle();
        }
    }
}
