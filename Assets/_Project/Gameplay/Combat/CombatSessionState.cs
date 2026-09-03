using LastSeed.Gameplay.Signals;
using Zenject;

namespace LastSeed.Gameplay.Combat
{
    public sealed class CombatSessionState : ICombatSessionState
    {
        private readonly SignalBus _signalBus;

        public CombatSessionState(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        public bool IsShootingEnabled { get; private set; }

        public void SetShootingEnabled(bool isEnabled)
        {
            if (IsShootingEnabled == isEnabled)
                return;

            IsShootingEnabled = isEnabled;
            _signalBus.Fire(new CombatShootingStateChangedSignal(isEnabled));
        }

        public void Reset()
        {
            SetShootingEnabled(false);
        }
    }
}
