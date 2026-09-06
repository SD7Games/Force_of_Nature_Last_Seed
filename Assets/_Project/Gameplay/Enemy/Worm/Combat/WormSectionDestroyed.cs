public readonly struct WormSectionDestroyed
{
    public WormSectionDestroyed(
        WormSection section,
        in WormSectionHealthChange finalChange)
    {
        Section = section;
        FinalChange = finalChange;
    }

    public WormSection Section { get; }
    public WormSectionHealthChange FinalChange { get; }
}
