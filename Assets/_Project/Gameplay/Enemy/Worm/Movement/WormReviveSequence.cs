using System;
using System.Collections.Generic;

public sealed class WormReviveSequence
{
    private readonly WormReviveAnimationController _animationController;
    private readonly WormReviveVisualScaler _visualScaler;

    private IReadOnlyList<WormSegment> _segments;
    private Action _completion;
    private bool _awaitingCompletion;

    public WormReviveSequence(
        WormReviveAnimationController animationController,
        WormReviveVisualScaler visualScaler)
    {
        _animationController = animationController
            ?? throw new ArgumentNullException(nameof(animationController));
        _visualScaler = visualScaler
            ?? throw new ArgumentNullException(nameof(visualScaler));
    }

    public bool IsActive => _animationController.IsActive;
    public float VisualYOffset { get; private set; }

    public void Begin(
        float startDistance,
        float targetDistance,
        in WormReviveAnimationSettings settings,
        IReadOnlyList<WormSegment> segments,
        Action onCompleted)
    {
        if (segments == null)
            throw new ArgumentNullException(nameof(segments));

        Cancel();
        _segments = segments;
        _completion = onCompleted;
        _visualScaler.Capture(segments);
        _animationController.Begin(startDistance, targetDistance, settings);
    }

    public WormReviveAnimationFrame Advance(float deltaTime)
    {
        WormReviveAnimationFrame frame = _animationController.Advance(deltaTime);
        VisualYOffset = frame.VisualYOffset;

        if (_segments != null)
            _visualScaler.Apply(_segments, frame.Scale.X, frame.Scale.Y);

        _awaitingCompletion = frame.Completed;
        return frame;
    }

    public void CompleteAfterFinalRender(Action renderFinalState)
    {
        if (!_awaitingCompletion)
            return;

        renderFinalState?.Invoke();

        Action completion = _completion;
        ClearVisualState();
        completion?.Invoke();
    }

    public void Cancel()
    {
        _animationController.Cancel();
        ClearVisualState();
    }

    private void ClearVisualState()
    {
        VisualYOffset = 0f;

        if (_segments != null)
            _visualScaler.RestoreAndClear(_segments);

        _segments = null;
        _completion = null;
        _awaitingCompletion = false;
    }
}
