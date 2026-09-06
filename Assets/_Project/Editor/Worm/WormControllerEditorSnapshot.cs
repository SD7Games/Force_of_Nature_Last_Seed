internal readonly struct WormControllerEditorSnapshot
{
    public WormControllerEditorSnapshot(
        RailPath rail,
        float speed,
        float segmentSpacing,
        float rollbackSpeed,
        float sectionRollbackForwardSpeedMultiplier,
        float reviveRollbackProgressNormalized)
    {
        Rail = rail;
        Speed = speed;
        SegmentSpacing = segmentSpacing;
        RollbackSpeed = rollbackSpeed;
        SectionRollbackForwardSpeedMultiplier = sectionRollbackForwardSpeedMultiplier;
        ReviveRollbackProgressNormalized = reviveRollbackProgressNormalized;
    }

    public RailPath Rail { get; }
    public float Speed { get; }
    public float SegmentSpacing { get; }
    public float RollbackSpeed { get; }
    public float SectionRollbackForwardSpeedMultiplier { get; }
    public float ReviveRollbackProgressNormalized { get; }
}
