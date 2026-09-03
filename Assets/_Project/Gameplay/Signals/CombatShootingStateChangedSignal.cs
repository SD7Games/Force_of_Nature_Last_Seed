namespace LastSeed.Gameplay.Signals
{
    public sealed class CombatShootingStateChangedSignal
    {
        public CombatShootingStateChangedSignal(bool isShootingEnabled)
        {
            IsShootingEnabled = isShootingEnabled;
        }

        public bool IsShootingEnabled { get; }
    }
}
