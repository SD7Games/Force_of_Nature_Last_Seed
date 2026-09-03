using System;
using DG.Tweening;
using UnityEngine;

public sealed class WormCocoonShakeClock : IWormCocoonShakeClock, IDisposable
{
    private const int ShakeStepCount = 8;
    private const float ShakeStepDuration = 0.1f;
    private const float ShakeDuration = ShakeStepDuration * ShakeStepCount;

    private readonly object _tweenTarget = new();

    private Sequence _sequence;
    private float _rotationOffset;
    private int _subscriberCount;

    public float RotationOffset => _rotationOffset;

    public void Register(float interval, float angle)
    {
        if (angle <= 0f)
            return;

        _subscriberCount++;

        if (_sequence == null || !_sequence.IsActive())
            StartSequence(interval, angle);
    }

    public void Unregister()
    {
        _subscriberCount = Mathf.Max(0, _subscriberCount - 1);

        if (_subscriberCount == 0)
            StopSequence();
    }

    public void Dispose()
    {
        _subscriberCount = 0;
        StopSequence();
    }

    private void StartSequence(float interval, float angle)
    {
        float strongAngle = angle * 0.8f;
        float mediumAngle = angle * 0.55f;
        float weakAngle = angle * 0.3f;
        float shakeDelay = Mathf.Max(0f, interval - ShakeDuration);

        _sequence = DOTween.Sequence()
            .SetTarget(_tweenTarget)
            .AppendInterval(shakeDelay)
            .Append(CreateTween(angle))
            .Append(CreateTween(-angle))
            .Append(CreateTween(strongAngle))
            .Append(CreateTween(-strongAngle))
            .Append(CreateTween(mediumAngle))
            .Append(CreateTween(-mediumAngle))
            .Append(CreateTween(weakAngle))
            .Append(CreateTween(0f))
            .SetLoops(-1, LoopType.Restart);
    }

    private Tween CreateTween(float targetAngle)
    {
        return DOTween
            .To(
                () => _rotationOffset,
                value => _rotationOffset = value,
                targetAngle,
                ShakeStepDuration)
            .SetEase(Ease.InOutSine);
    }

    private void StopSequence()
    {
        if (_sequence != null)
        {
            _sequence.Kill();
            _sequence = null;
        }

        _rotationOffset = 0f;
    }
}
