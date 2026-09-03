using LastSeed.Gameplay.Combat;
using Zenject;

namespace LastSeed.Bootstrap.Installers
{
    public sealed class GameSessionInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<CombatSessionState>()
                .AsSingle();
        }
    }
}
