public readonly struct WormFrameContext
{
    public WormFrameContext(
        RailPath rail,
        WormForwardMotionSettings forwardMotion,
        WormSegmentChainLayout segmentLayout,
        float baseSpeed,
        float rollbackForwardSpeedMultiplier,
        float rollbackSpeed,
        float deltaTime,
        float unscaledDeltaTime)
    {
        Rail = rail;
        ForwardMotion = forwardMotion;
        SegmentLayout = segmentLayout;
        BaseSpeed = baseSpeed;
        RollbackForwardSpeedMultiplier = rollbackForwardSpeedMultiplier;
        RollbackSpeed = rollbackSpeed;
        DeltaTime = deltaTime;
        UnscaledDeltaTime = unscaledDeltaTime;
    }

    public RailPath Rail { get; }
    public WormForwardMotionSettings ForwardMotion { get; }
    public WormSegmentChainLayout SegmentLayout { get; }
    public float BaseSpeed { get; }
    public float RollbackForwardSpeedMultiplier { get; }
    public float RollbackSpeed { get; }
    public float DeltaTime { get; }
    public float UnscaledDeltaTime { get; }
}
