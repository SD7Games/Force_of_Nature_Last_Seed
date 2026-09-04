public readonly struct WormForwardMotionSettings
{
    public WormForwardMotionSettings(
        float baseSpeed,
        float catchUpSpeed,
        int catchUpRailPointIndex,
        float catchUpStopOffset,
        float catchUpExtraDistance,
        int burstDisableRailPointIndex,
        float burstDisablePathProgress,
        in WormCombatBurstSettings burstSettings)
    {
        BaseSpeed = baseSpeed;
        CatchUpSpeed = catchUpSpeed;
        CatchUpRailPointIndex = catchUpRailPointIndex;
        CatchUpStopOffset = catchUpStopOffset;
        CatchUpExtraDistance = catchUpExtraDistance;
        BurstDisableRailPointIndex = burstDisableRailPointIndex;
        BurstDisablePathProgress = burstDisablePathProgress;
        BurstSettings = burstSettings;
    }

    public float BaseSpeed { get; }
    public float CatchUpSpeed { get; }
    public int CatchUpRailPointIndex { get; }
    public float CatchUpStopOffset { get; }
    public float CatchUpExtraDistance { get; }
    public int BurstDisableRailPointIndex { get; }
    public float BurstDisablePathProgress { get; }
    public WormCombatBurstSettings BurstSettings { get; }
}
