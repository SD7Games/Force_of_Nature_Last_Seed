using System;

public sealed class WormRailTargetResolver
{
    private IWormRailPath _catchUpRail;
    private int _catchUpPointIndex = -1;
    private float _catchUpDistance;

    private IWormRailPath _reviveRail;
    private int _revivePointIndex = -2;
    private float _reviveDistance;

    private IWormRailPath _burstDisableRail;
    private int _burstDisablePointIndex = -2;
    private float _burstDisableDistance;

    public bool TryGetCatchUpDistance(
        IWormRailPath rail,
        int pointIndex,
        out float distance)
    {
        distance = 0f;

        if (rail == null)
            return false;

        if (_catchUpRail == rail && _catchUpPointIndex == pointIndex)
        {
            distance = _catchUpDistance;
            return true;
        }

        if (!rail.TryGetControlPointDistance(pointIndex, out distance))
            return false;

        _catchUpRail = rail;
        _catchUpPointIndex = pointIndex;
        _catchUpDistance = distance;
        return true;
    }

    public bool TryGetReviveDistance(
        IWormRailPath rail,
        int revivePointIndex,
        int fallbackPointIndex,
        out float distance)
    {
        distance = 0f;

        if (rail == null)
            return false;

        int targetPointIndex = revivePointIndex >= 0
            ? revivePointIndex
            : fallbackPointIndex;

        if (_reviveRail == rail && _revivePointIndex == targetPointIndex)
        {
            distance = _reviveDistance;
            return true;
        }

        if (!rail.TryGetControlPointDistance(targetPointIndex, out distance))
            return false;

        _reviveRail = rail;
        _revivePointIndex = targetPointIndex;
        _reviveDistance = distance;
        return true;
    }

    public bool TryGetBurstDisableDistance(
        IWormRailPath rail,
        int pointIndex,
        float fallbackPathProgress,
        out float distance)
    {
        distance = 0f;

        if (rail == null || rail.TotalLength <= 0f)
            return false;

        if (pointIndex >= 0)
        {
            if (_burstDisableRail == rail && _burstDisablePointIndex == pointIndex)
            {
                distance = _burstDisableDistance;
                return true;
            }

            if (rail.TryGetControlPointDistance(pointIndex, out distance))
            {
                _burstDisableRail = rail;
                _burstDisablePointIndex = pointIndex;
                _burstDisableDistance = distance;
                return true;
            }
        }

        distance = Math.Max(0f, Math.Min(1f, fallbackPathProgress)) * rail.TotalLength;
        return true;
    }

    public void Clear()
    {
        _catchUpRail = null;
        _catchUpPointIndex = -1;
        _catchUpDistance = 0f;
        _reviveRail = null;
        _revivePointIndex = -2;
        _reviveDistance = 0f;
        _burstDisableRail = null;
        _burstDisablePointIndex = -2;
        _burstDisableDistance = 0f;
    }
}
