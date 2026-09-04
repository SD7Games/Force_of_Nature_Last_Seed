public readonly struct WormReviveAnimationFrame
{
    public WormReviveAnimationFrame(
        float headDistance,
        float visualYOffset,
        in WormScale2 scale,
        bool completed)
    {
        HeadDistance = headDistance;
        VisualYOffset = visualYOffset;
        Scale = scale;
        Completed = completed;
    }

    public float HeadDistance { get; }
    public float VisualYOffset { get; }
    public WormScale2 Scale { get; }
    public bool Completed { get; }
}
