using LastSeed.Bootstrap.Gameplay;
using LastSeed.Bootstrap.GameplayLoop;
using Zenject;

namespace LastSeed.Bootstrap.Installers
{
    public sealed class GameplayLoopInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<GameplayFrameCoordinator>().AsSingle();

            Container
                .Bind<GameplayUpdateDriver>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName(nameof(GameplayUpdateDriver))
                .AsSingle()
                .NonLazy();

            Container
                .BindInterfacesTo<GameSceneInitializer>()
                .AsSingle();
        }
    }
}
