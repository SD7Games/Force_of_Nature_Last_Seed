namespace LastSeed.Gameplay.Signals
{
    public sealed class WeaponAttackCycleStartedSignal
    {
        public WeaponAttackCycleStartedSignal(
            float currentCooldown,
            float baseCooldown)
        {
            CurrentCooldown = currentCooldown;
            BaseCooldown = baseCooldown;
        }

        public float CurrentCooldown { get; }
        public float BaseCooldown { get; }
    }
}
