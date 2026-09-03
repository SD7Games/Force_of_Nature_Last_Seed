using LastSeed.Gameplay.Input;
using LastSeed.Infrastructure.Input;
using UnityEngine;
using Zenject;

namespace LastSeed.Bootstrap.Installers
{
    public sealed class GameplayInputInstaller : MonoInstaller
    {
        [SerializeField] private PlayerInputSnapshotProvider _playerInputSnapshotProvider;

        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<PlayerInputSnapshotProvider>()
                .FromInstance(_playerInputSnapshotProvider)
                .AsSingle();

            Container
                .BindInterfacesAndSelfTo<GameplayInputLock>()
                .AsSingle();
        }
    }
}
