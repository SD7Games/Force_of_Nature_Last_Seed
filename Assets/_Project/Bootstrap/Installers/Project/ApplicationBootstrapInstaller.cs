using LastSeed.Bootstrap.Application;
using Zenject;

namespace LastSeed.Bootstrap.Installers
{
    public sealed class ApplicationBootstrapInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<InitialSceneBootstrapper>().AsSingle();
        }
    }
}
