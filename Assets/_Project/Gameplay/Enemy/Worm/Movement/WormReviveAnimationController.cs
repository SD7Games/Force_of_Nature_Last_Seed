using System;

public sealed class WormReviveAnimationController
{
    private const float MinimumDuration = 0.01f;
    private const float MinimumRollbackDistance = 0.001f;
    private const float LandingBounceHeightMultiplier = 0.12f;

    private readonly WormReviveMotionCalculator _motionCalculator;

    private WormReviveAnimationSettings _settings;
    private WormReviveAnimationPhase _phase;
    private float _elapsed;
    private float _headDistance;
    private float _targetDistance;
    private float _rollbackDistance;
    private float _cruiseSpeed;

    public WormReviveAnimationController(WormReviveMotionCalculator motionCalculator)
    {
        _motionCalculator = motionCalculator;
    }

    public bool IsActive => _phase != WormReviveAnimationPhase.Inactive;

    public void Begin(
        float startDistance,
        float targetDistance,
        in WormReviveAnimationSettings settings)
    {
        _settings = settings;
        _headDistance = Math.Max(targetDistance, startDistance);
        _targetDistance = targetDistance;
        _rollbackDistance = Math.Max(0f, _headDistance - _targetDistance);
        _cruiseSpeed = _motionCalculator.CalculateCruiseSpeed(
            _rollbackDistance,
            settings.ThrowDuration,
            settings.DecelerationPathFraction,
            settings.GameplaySpeed);
        _elapsed = 0f;
        _phase = WormReviveAnimationPhase.Squash;
    }

    public WormReviveAnimationFrame Advance(float deltaTime)
    {
        float safeDeltaTime = Math.Max(0f, deltaTime);

        return _phase switch
        {
            WormReviveAnimationPhase.Squash => AdvanceSquash(safeDeltaTime),
            WormReviveAnimationPhase.Throw => AdvanceThrow(safeDeltaTime),
            WormReviveAnimationPhase.Landing => AdvanceLanding(safeDeltaTime),
            _ => new WormReviveAnimationFrame(
                _headDistance,
                0f,
                new WormScale2(1f, 1f),
                false)
        };
    }

    public void Cancel()
    {
        _phase = WormReviveAnimationPhase.Inactive;
        _elapsed = 0f;
    }

    private WormReviveAnimationFrame AdvanceSquash(float deltaTime)
    {
        float progress = AdvancePhaseTime(deltaTime, _settings.SquashDuration);
        float eased = _motionCalculator.EaseOutCubic(progress);
        WormScale2 scale = new(
            LerpUnclamped(1f, _settings.SquashXScale, eased),
            LerpUnclamped(1f, _settings.SquashYScale, eased));

        if (progress >= 1f)
            EnterPhase(WormReviveAnimationPhase.Throw);

        return new WormReviveAnimationFrame(_headDistance, 0f, scale, false);
    }

    private WormReviveAnimationFrame AdvanceThrow(float deltaTime)
    {
        if (_rollbackDistance <= MinimumRollbackDistance)
            return EnterLanding();

        float remainingDistance = Math.Max(0f, _headDistance - _targetDistance);
        float speed = _motionCalculator.CalculateThrowSpeed(
            remainingDistance,
            _rollbackDistance,
            _cruiseSpeed,
            _settings.DecelerationPathFraction,
            _settings.GameplaySpeed);
        _headDistance = Math.Max(_targetDistance, _headDistance - speed * deltaTime);
        remainingDistance = Math.Max(0f, _headDistance - _targetDistance);
        float progress = 1f - Clamp01(remainingDistance / _rollbackDistance);
        float visualYOffset = (float)Math.Sin(progress * Math.PI) * _settings.ArcHeight;
        WormScale2 scale = _motionCalculator.CalculateTravelScale(
            progress,
            _settings.SquashXScale,
            _settings.SquashYScale);

        if (_headDistance <= _targetDistance)
            EnterPhase(WormReviveAnimationPhase.Landing);

        return new WormReviveAnimationFrame(
            _headDistance,
            visualYOffset,
            scale,
            false);
    }

    private WormReviveAnimationFrame EnterLanding()
    {
        _headDistance = _targetDistance;
        EnterPhase(WormReviveAnimationPhase.Landing);
        return new WormReviveAnimationFrame(
            _headDistance,
            0f,
            new WormScale2(1f, 1f),
            false);
    }

    private WormReviveAnimationFrame AdvanceLanding(float deltaTime)
    {
        float progress = AdvancePhaseTime(deltaTime, _settings.LandingDuration);
        float eased = _motionCalculator.EaseOutBack(progress);
        float visualYOffset = (float)Math.Sin(progress * Math.PI)
            * (_settings.ArcHeight * LandingBounceHeightMultiplier);
        WormScale2 scale = new(
            LerpUnclamped(_settings.LandingXScale, 1f, eased),
            LerpUnclamped(_settings.LandingYScale, 1f, eased));
        bool completed = progress >= 1f;

        if (completed)
        {
            Cancel();
            visualYOffset = 0f;
            scale = new WormScale2(1f, 1f);
        }

        return new WormReviveAnimationFrame(
            _targetDistance,
            visualYOffset,
            scale,
            completed);
    }

    private float AdvancePhaseTime(float deltaTime, float duration)
    {
        _elapsed += deltaTime;
        return Clamp01(_elapsed / Math.Max(MinimumDuration, duration));
    }

    private void EnterPhase(WormReviveAnimationPhase phase)
    {
        _phase = phase;
        _elapsed = 0f;
    }

    private static float Clamp01(float value)
    {
        return Math.Max(0f, Math.Min(1f, value));
    }

    private static float LerpUnclamped(float from, float to, float time)
    {
        return from + (to - from) * time;
    }

    private enum WormReviveAnimationPhase
    {
        Inactive,
        Squash,
        Throw,
        Landing
    }
}
