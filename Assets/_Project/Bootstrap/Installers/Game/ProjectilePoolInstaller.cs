using UnityEngine;
using Zenject;

namespace LastSeed.Bootstrap.Installers
{
    public sealed class ProjectilePoolInstaller : MonoInstaller
    {
        [SerializeField] private PoolRegistry _projectilePoolRegistry;

        public override void InstallBindings()
        {
            Container.Bind<PoolRegistry>().FromInstance(_projectilePoolRegistry).AsSingle();
        }
    }
}
