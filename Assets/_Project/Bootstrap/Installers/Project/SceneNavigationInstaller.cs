using LastSeed.Infrastructure.Navigation;
using Zenject;

namespace LastSeed.Bootstrap.Installers
{
    public sealed class SceneNavigationInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .Bind<SceneRouteCatalog<GameSceneId>>()
                .FromInstance(GameSceneRoutes.CreateCatalog());

            Container
                .Bind<ISceneLoader>()
                .To<UnitySceneLoader>()
                .AsSingle();

            Container
                .Bind<ISceneNavigator<GameSceneId>>()
                .To<SceneNavigator<GameSceneId>>()
                .AsSingle();
        }
    }
}
