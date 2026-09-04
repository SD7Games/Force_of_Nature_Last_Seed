public readonly struct WormSectionRollbackMotionResult
{
    public WormSectionRollbackMotionResult(float headDistance, bool completed)
    {
        HeadDistance = headDistance;
        Completed = completed;
    }

    public float HeadDistance { get; }
    public bool Completed { get; }
}
