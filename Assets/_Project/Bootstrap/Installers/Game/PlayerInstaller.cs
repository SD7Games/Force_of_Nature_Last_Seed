using UnityEngine;
using Zenject;

namespace LastSeed.Bootstrap.Installers
{
    public sealed class PlayerInstaller : MonoInstaller
    {
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PlayerMover _playerMover;
        [SerializeField] private PlayerShooter _playerShooter;

        public override void InstallBindings()
        {
            Container.Bind<PlayerController>().FromInstance(_playerController).AsSingle();
            Container.Bind<PlayerMover>().FromInstance(_playerMover).AsSingle();
            Container.Bind<PlayerShooter>().FromInstance(_playerShooter).AsSingle();
        }
    }
}
