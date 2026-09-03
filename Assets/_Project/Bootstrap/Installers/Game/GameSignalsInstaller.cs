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
            Container.DeclareSignal<WormCombatBurstStateChangedSignal>();
            Container.DeclareSignal<WormDamageDealtSignal>();
            Container.DeclareSignal<WormDestructionProgressChangedSignal>();
            Container.DeclareSignal<WormPathCompletedSignal>();
            Container.DeclareSignal<WeaponRuntimeStatsChangedSignal>();
            Container.DeclareSignal<WeaponAttackCycleStartedSignal>();
            Container.DeclareSignal<ShowPopupRequestedSignal>();
        }
    }
}
