using System;
using System.Collections.Generic;

public sealed class WormSectionRollbackMotionController<TSegment>
    where TSegment : class
{
    private readonly WormSectionRollbackState<TSegment> _state;

    public WormSectionRollbackMotionController(WormSectionRollbackState<TSegment> state)
    {
        _state = state;
    }

    public WormSectionRollbackMotionResult Advance(
        float headDistance,
        IReadOnlyList<TSegment> segments,
        float railLength,
        float baseSpeed,
        float anchoredTailSpeedMultiplier,
        float rollbackSpeed,
        float deltaTime)
    {
        if (!_state.IsActive)
            return new WormSectionRollbackMotionResult(headDistance, false);

        float safeDeltaTime = Math.Max(0f, deltaTime);
        _state.AdvanceAnchoredTail(
            segments,
            railLength,
            baseSpeed,
            anchoredTailSpeedMultiplier,
            safeDeltaTime);

        float targetDistance = _state.TargetDistance;

        if (headDistance > targetDistance)
        {
            headDistance = MoveTowards(
                headDistance,
                targetDistance,
                Math.Max(0f, rollbackSpeed) * safeDeltaTime);
        }

        bool completed = headDistance <= targetDistance;
        return new WormSectionRollbackMotionResult(
            completed ? Math.Min(headDistance, targetDistance) : headDistance,
            completed);
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        if (Math.Abs(target - current) <= maxDelta)
            return target;

        return current + Math.Sign(target - current) * maxDelta;
    }
}
