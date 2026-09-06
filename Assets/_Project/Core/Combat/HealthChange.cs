namespace LastSeed.Core.Combat
{
    public readonly struct HealthChange
    {
        public HealthChange(
            int previousHp,
            int currentHp,
            int maxHp,
            int appliedDamage,
            bool isReset)
        {
            PreviousHp = previousHp;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            AppliedDamage = appliedDamage;
            IsReset = isReset;
        }

        public int PreviousHp { get; }
        public int CurrentHp { get; }
        public int MaxHp { get; }
        public int AppliedDamage { get; }
        public bool IsReset { get; }
        public bool IsDepleted => CurrentHp <= 0;
    }
}
