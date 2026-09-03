using LastSeed.Gameplay.Signals;
using LastSeed.Presentation.UI.Popups;
using Zenject;

namespace LastSeed.Bootstrap.Installers
{
    public sealed class GameSignalsInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            SignalBusInstaller.Install(Container);

            Container.DeclareSignal<CombatShootingStateChangedSignal>();
            Container.DeclareSignal<WormDiedSignal>();
            Container.DeclareSignal<WormRewardRequestedSignal>();
            Container.DeclareSignal<WormReviveGrantedSignal>();
            Container.DeclareSignal<WormReviveRollbackCompletedSignal>();
            Container.DeclareSignal<ShowPopupRequestedSignal>();
        }
    }
}
