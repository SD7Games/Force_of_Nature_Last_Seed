using LastSeed.Core.Combat;

public readonly struct WormSectionHealthChanged
{
    public WormSectionHealthChanged(
        WormSection section,
        in HealthChange change)
    {
        Section = section;
        Change = change;
    }

    public WormSection Section { get; }
    public HealthChange Change { get; }
}
