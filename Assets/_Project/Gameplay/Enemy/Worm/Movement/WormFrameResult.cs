public readonly struct WormFrameResult
{
    public WormFrameResult(bool pathCompleted, float headPathProgressNormalized)
    {
        PathCompleted = pathCompleted;
        HeadPathProgressNormalized = headPathProgressNormalized;
    }

    public bool PathCompleted { get; }
    public float HeadPathProgressNormalized { get; }
}
