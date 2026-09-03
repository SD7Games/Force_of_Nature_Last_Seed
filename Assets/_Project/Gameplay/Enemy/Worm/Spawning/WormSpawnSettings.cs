using System;

public sealed class WormSpawnSettings
{
    public WormSpawnSettings(
        int sectionCount,
        int poolPadding,
        int prewarmBatchSize)
    {
        SectionCount = Math.Max(1, sectionCount);
        PoolPadding = Math.Max(0, poolPadding);
        PrewarmBatchSize = Math.Max(1, prewarmBatchSize);
    }

    public int SectionCount { get; }
    public int PoolPadding { get; }
    public int PrewarmBatchSize { get; }
    public int BodyPoolCapacity =>
        WormPatternBuilder.GetBodySegmentCount(SectionCount) + PoolPadding;
}
