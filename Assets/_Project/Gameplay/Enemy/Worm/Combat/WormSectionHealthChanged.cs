public readonly struct WormSectionHealthChanged
{
    public WormSectionHealthChanged(
        WormSection section,
        in WormSectionHealthChange change)
    {
        Section = section;
        Change = change;
    }

    public WormSection Section { get; }
    public WormSectionHealthChange Change { get; }
}
