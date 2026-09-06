using LastSeed.Core.Combat;

public readonly struct WormSectionDestroyed
{
    public WormSectionDestroyed(
        WormSection section,
        in HealthChange finalChange)
    {
        Section = section;
        FinalChange = finalChange;
    }

    public WormSection Section { get; }
    public HealthChange FinalChange { get; }
}
