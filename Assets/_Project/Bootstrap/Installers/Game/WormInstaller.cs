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

        [Header("Adaptive HP")]
        [SerializeField] private WormHpScalingConfig _hpScalingConfig;
        [SerializeField, Min(1)] private int _levelNumber = 1;
        [SerializeField, Min(1)] private int _upgradeRebalanceInterval = 1;
        [SerializeField, Min(0f)] private float _minimumRebalanceInterval = 5f;

#if UNITY_EDITOR
        public WormHpScalingConfig EditorHpScalingConfig => _hpScalingConfig;
        public int EditorLevelNumber => _levelNumber;
#endif

        public override void InstallBindings()
        {
            Container.Bind<WormSpawner>().FromInstance(_wormSpawner).AsSingle();
            Container.Bind<WormController>().FromInstance(_wormController).AsSingle();
            Container.Bind<WormCombatController>().FromInstance(_wormCombatController).AsSingle();
            Container.Bind<IWormPathProgressProvider>().FromInstance(_wormController).AsSingle();
            Container.Bind<IWormHpScalingPolicy>().FromInstance(_hpScalingConfig).AsSingle();
            Container.Bind<IWeaponPowerProvider>().To<WeaponPowerProvider>().AsSingle();
            Container.BindInstance(new WormAdaptiveHpSettings(
                _levelNumber,
                _upgradeRebalanceInterval,
                _minimumRebalanceInterval));
            Container.Bind<WormAdaptiveHpController>().AsSingle();
        }
    }
}
