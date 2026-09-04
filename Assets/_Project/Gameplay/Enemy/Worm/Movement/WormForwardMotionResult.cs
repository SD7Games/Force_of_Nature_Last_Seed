public readonly struct WormForwardMotionResult
{
    public WormForwardMotionResult(
        float headDistance,
        bool isCatchingUp,
        bool completedPath)
    {
        HeadDistance = headDistance;
        IsCatchingUp = isCatchingUp;
        CompletedPath = completedPath;
    }

    public float HeadDistance { get; }
    public bool IsCatchingUp { get; }
    public bool CompletedPath { get; }
}
