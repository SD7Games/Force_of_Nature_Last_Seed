using System;

public sealed class WormForwardMotionController
{
    private readonly WormCombatBurstController _combatBurstController;
    private readonly WormRailTargetResolver _railTargetResolver;

    public WormForwardMotionController(
        WormCombatBurstController combatBurstController,
        WormRailTargetResolver railTargetResolver)
    {
        _combatBurstController = combatBurstController ??
            throw new ArgumentNullException(nameof(combatBurstController));
        _railTargetResolver = railTargetResolver ??
            throw new ArgumentNullException(nameof(railTargetResolver));
    }

    public WormForwardMotionResult Advance(
        float headDistance,
        float deltaTime,
        IWormRailPath rail,
        in WormForwardMotionSettings settings)
    {
        if (deltaTime <= 0f || rail == null || rail.TotalLength <= 0f)
            return new WormForwardMotionResult(headDistance, false, false);

        float previousDistance = headDistance;
        bool isCatchingUp = ShouldCatchUp(headDistance, rail, settings);
        bool canUseBurst = CanUseCombatBurst(
            headDistance,
            deltaTime,
            rail,
            settings);
        float speed = _combatBurstController.ResolveForwardSpeed(
            deltaTime,
            settings.BaseSpeed,
            settings.CatchUpSpeed,
            isCatchingUp,
            canUseBurst,
            settings.BurstSettings);
        float targetDistance = rail.TotalLength;
        float nextDistance = Math.Min(
            targetDistance,
            headDistance + speed * deltaTime);
        bool completedPath = previousDistance < targetDistance &&
            nextDistance >= targetDistance;

        return new WormForwardMotionResult(
            nextDistance,
            isCatchingUp,
            completedPath);
    }

    private bool ShouldCatchUp(
        float headDistance,
        IWormRailPath rail,
        in WormForwardMotionSettings settings)
    {
        if (!_railTargetResolver.TryGetCatchUpDistance(
                rail,
                settings.CatchUpRailPointIndex,
                out float targetDistance))
        {
            return false;
        }

        targetDistance = Math.Max(
            0f,
            targetDistance - settings.CatchUpStopOffset + settings.CatchUpExtraDistance);

        return headDistance < targetDistance;
    }

    private bool CanUseCombatBurst(
        float headDistance,
        float deltaTime,
        IWormRailPath rail,
        in WormForwardMotionSettings settings)
    {
        if (!_railTargetResolver.TryGetBurstDisableDistance(
                rail,
                settings.BurstDisableRailPointIndex,
                settings.BurstDisablePathProgress,
                out float disableDistance))
        {
            return true;
        }

        float projectedDistance = headDistance +
            Math.Max(settings.BaseSpeed, settings.BurstSettings.BurstSpeed) *
            Math.Max(0f, deltaTime);

        return projectedDistance < disableDistance;
    }
}
