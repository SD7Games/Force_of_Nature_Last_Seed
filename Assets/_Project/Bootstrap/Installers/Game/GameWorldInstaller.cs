using UnityEngine;
using Zenject;

namespace LastSeed.Bootstrap.Installers
{
    public sealed class GameWorldInstaller : MonoInstaller
    {
        [Header("World")]
        [SerializeField] private Camera _worldCamera;

        public override void InstallBindings()
        {
            Container.Bind<Camera>().FromInstance(_worldCamera).AsSingle();
            Container.Bind<IRandomSource>().To<UnityRandomSource>().AsSingle();

            Container
                .BindInterfacesAndSelfTo<ScreenBoundsService>()
                .AsSingle();
        }
    }
}
