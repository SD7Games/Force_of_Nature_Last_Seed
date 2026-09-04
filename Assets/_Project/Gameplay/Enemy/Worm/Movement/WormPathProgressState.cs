public sealed class WormPathProgressState
{
    private bool _hasCompletedPath;

    public float HeadDistance { get; private set; }
    public bool IsCatchingUp { get; private set; }

    public void Reset(bool isCatchingUp = false)
    {
        HeadDistance = 0f;
        IsCatchingUp = isCatchingUp;
        _hasCompletedPath = false;
    }

    public bool Apply(in WormForwardMotionResult result)
    {
        HeadDistance = result.HeadDistance;
        IsCatchingUp = result.IsCatchingUp;
        return TryComplete(result.CompletedPath);
    }

    public void SetHeadDistance(float headDistance)
    {
        HeadDistance = headDistance;
    }

    public void ReopenPath()
    {
        _hasCompletedPath = false;
    }

    public bool TryComplete(bool completedPath)
    {
        if (!completedPath || _hasCompletedPath)
            return false;

        _hasCompletedPath = true;
        return true;
    }
}
