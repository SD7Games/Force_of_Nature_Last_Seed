using LastSeed.Bootstrap.Gameplay;
using UnityEngine;
using Zenject;

namespace LastSeed.Bootstrap.Installers
{
    public sealed class GameWorldInstaller : MonoInstaller
    {
        [Header("World")]
        [SerializeField] private Camera _worldCamera;

        [Header("Player")]
        [SerializeField] private PlayerMover _playerMover;
        [SerializeField] private PlayerShooter _playerShooter;

        [Header("Projectiles")]
        [SerializeField] private PoolRegistry _projectilePoolRegistry;

        public override void InstallBindings()
        {
            Container.Bind<Camera>().FromInstance(_worldCamera).AsSingle();
            Container.Bind<PlayerMover>().FromInstance(_playerMover).AsSingle();
            Container.Bind<PlayerShooter>().FromInstance(_playerShooter).AsSingle();
            Container.Bind<PoolRegistry>().FromInstance(_projectilePoolRegistry).AsSingle();

            Container
                .BindInterfacesAndSelfTo<ScreenBoundsService>()
                .AsSingle();

            Container
                .BindInterfacesTo<GameWorldInitializer>()
                .AsSingle();
        }
    }
}
