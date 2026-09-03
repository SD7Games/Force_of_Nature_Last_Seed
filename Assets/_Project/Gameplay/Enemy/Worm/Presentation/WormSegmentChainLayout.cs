public readonly struct WormSegmentChainLayout
{
    public WormSegmentChainLayout(
        float headDistance,
        float segmentSpacing,
        float tailVisualSpacingMultiplier,
        float headBridgeSpacingMultiplier,
        float activeDistancePadding,
        float waveAmplitude,
        float waveFrequency,
        float waveTime,
        float verticalOffset,
        bool isSectionRollback,
        bool isReviveRollback)
    {
        HeadDistance = headDistance;
        SegmentSpacing = segmentSpacing;
        TailVisualSpacingMultiplier = tailVisualSpacingMultiplier;
        HeadBridgeSpacingMultiplier = headBridgeSpacingMultiplier;
        ActiveDistancePadding = activeDistancePadding;
        WaveAmplitude = waveAmplitude;
        WaveFrequency = waveFrequency;
        WaveTime = waveTime;
        VerticalOffset = verticalOffset;
        IsSectionRollback = isSectionRollback;
        IsReviveRollback = isReviveRollback;
    }

    public float HeadDistance { get; }
    public float SegmentSpacing { get; }
    public float TailVisualSpacingMultiplier { get; }
    public float HeadBridgeSpacingMultiplier { get; }
    public float ActiveDistancePadding { get; }
    public float WaveAmplitude { get; }
    public float WaveFrequency { get; }
    public float WaveTime { get; }
    public float VerticalOffset { get; }
    public bool IsSectionRollback { get; }
    public bool IsReviveRollback { get; }
}
