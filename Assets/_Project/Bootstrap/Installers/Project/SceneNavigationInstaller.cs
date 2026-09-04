using LastSeed.Infrastructure.Navigation;
using Zenject;

namespace LastSeed.Bootstrap.Installers
{
    public sealed class SceneNavigationInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<UnitySceneLoader>().AsSingle();
            Container.Bind<SceneLoadReadinessMonitor>().AsSingle();

            Container
                .Bind<ISceneNavigationService>()
                .To<SceneNavigationService>()
                .AsSingle();
        }
    }
}
