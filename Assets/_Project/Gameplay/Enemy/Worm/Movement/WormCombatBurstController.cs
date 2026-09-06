using System;

public sealed class WormCombatBurstController
{
    private const float ActiveSpeedTolerance = 0.01f;

    private float _intervalTimer;
    private float _remainingDuration;
    private float _currentForwardSpeed;
    private bool _hasReachedCombatStart;

    public bool IsActive { get; private set; }

    public void Reset(float baseSpeed)
    {
        _intervalTimer = 0f;
        _remainingDuration = 0f;
        _currentForwardSpeed = Math.Max(0f, baseSpeed);
        _hasReachedCombatStart = false;
        SetActive(false);
    }

    public float ResolveForwardSpeed(
        float deltaTime,
        float baseSpeed,
        float catchUpSpeed,
        bool isCatchingUp,
        bool canUseBurst,
        in WormCombatBurstSettings settings)
    {
        baseSpeed = Math.Max(0f, baseSpeed);

        if (isCatchingUp)
        {
            StopBurst();
            _currentForwardSpeed = baseSpeed;
            SetActive(false);
            return Math.Max(_currentForwardSpeed, catchUpSpeed);
        }

        UpdateBurst(deltaTime, canUseBurst, settings);

        float targetSpeed = _remainingDuration > 0f
            ? Math.Max(baseSpeed, settings.BurstSpeed)
            : baseSpeed;

        if (targetSpeed > baseSpeed)
        {
            _currentForwardSpeed = targetSpeed;
            SetActive(true);
            return _currentForwardSpeed;
        }

        float forwardSpeed = DecelerateToBaseSpeed(
            deltaTime,
            baseSpeed,
            settings.BurstSpeed,
            settings.SlowdownDuration);
        SetActive(forwardSpeed > baseSpeed + ActiveSpeedTolerance);
        return forwardSpeed;
    }

    private void UpdateBurst(
        float deltaTime,
        bool canUseBurst,
        in WormCombatBurstSettings settings)
    {
        if (!settings.Enabled || deltaTime <= 0f)
        {
            StopBurst();
            return;
        }

        if (!_hasReachedCombatStart)
        {
            _hasReachedCombatStart = true;
            _intervalTimer = 0f;
            _remainingDuration = 0f;
            return;
        }

        if (!canUseBurst)
        {
            StopBurst();
            return;
        }

        if (_remainingDuration > 0f)
        {
            _remainingDuration = Math.Max(0f, _remainingDuration - deltaTime);
            return;
        }

        _intervalTimer += deltaTime;

        if (_intervalTimer < settings.Interval)
            return;

        _intervalTimer = 0f;
        _remainingDuration = settings.Duration;
    }

    private void StopBurst()
    {
        _intervalTimer = 0f;
        _remainingDuration = 0f;
        SetActive(false);
    }

    private float DecelerateToBaseSpeed(
        float deltaTime,
        float baseSpeed,
        float burstSpeed,
        float slowdownDuration)
    {
        if (_currentForwardSpeed <= baseSpeed || deltaTime <= 0f)
        {
            _currentForwardSpeed = baseSpeed;
            return baseSpeed;
        }

        float maxBurstSpeed = Math.Max(baseSpeed, burstSpeed);
        float decelerationRate = (maxBurstSpeed - baseSpeed) /
            Math.Max(0.01f, slowdownDuration);
        float speedDelta = decelerationRate * deltaTime;

        _currentForwardSpeed = MoveTowards(
            _currentForwardSpeed,
            baseSpeed,
            speedDelta);

        return Math.Max(baseSpeed, _currentForwardSpeed);
    }

    private void SetActive(bool active)
    {
        if (IsActive == active)
            return;

        IsActive = active;
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        if (Math.Abs(target - current) <= maxDelta)
            return target;

        return current + Math.Sign(target - current) * maxDelta;
    }
}
