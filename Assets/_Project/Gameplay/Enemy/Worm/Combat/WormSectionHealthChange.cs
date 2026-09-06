public readonly struct WormSectionHealthChange
{
    public WormSectionHealthChange(
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
    public bool IsDestroyed => CurrentHp <= 0;
}
