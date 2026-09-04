public readonly struct WormReviveAnimationSettings
{
    public WormReviveAnimationSettings(
        float gameplaySpeed,
        float squashDuration,
        float throwDuration,
        float landingDuration,
        float decelerationPathFraction,
        float arcHeight,
        float squashXScale,
        float squashYScale,
        float landingXScale,
        float landingYScale)
    {
        GameplaySpeed = gameplaySpeed;
        SquashDuration = squashDuration;
        ThrowDuration = throwDuration;
        LandingDuration = landingDuration;
        DecelerationPathFraction = decelerationPathFraction;
        ArcHeight = arcHeight;
        SquashXScale = squashXScale;
        SquashYScale = squashYScale;
        LandingXScale = landingXScale;
        LandingYScale = landingYScale;
    }

    public float GameplaySpeed { get; }
    public float SquashDuration { get; }
    public float ThrowDuration { get; }
    public float LandingDuration { get; }
    public float DecelerationPathFraction { get; }
    public float ArcHeight { get; }
    public float SquashXScale { get; }
    public float SquashYScale { get; }
    public float LandingXScale { get; }
    public float LandingYScale { get; }
}
