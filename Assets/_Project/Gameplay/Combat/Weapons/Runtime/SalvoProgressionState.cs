using System;

public sealed class SalvoProgressionState
{
    private readonly int _hardLimit;
    private int _limit;

    public SalvoProgressionState(int defaultLimit, int hardLimit)
    {
        if (hardLimit < 0)
            throw new ArgumentOutOfRangeException(nameof(hardLimit));

        _hardLimit = hardLimit;
        _limit = Clamp(defaultLimit, 0, hardLimit);
    }

    private SalvoProgressionState(int limit, int hardLimit, int extraShots)
    {
        _hardLimit = hardLimit;
        _limit = limit;
        ExtraShots = extraShots;
    }

    public int ExtraShots { get; private set; }
    public bool CanAdd => ExtraShots < _limit;

    public void Reset()
    {
        ExtraShots = 0;
    }

    public void SetLimit(int limit)
    {
        _limit = Clamp(limit, 0, _hardLimit);
        ExtraShots = Math.Min(ExtraShots, _limit);
    }

    public bool CanApply(int extraShots)
    {
        return extraShots > 0 && ExtraShots + extraShots <= _limit;
    }

    public bool CanApply(int extraShots, int limitAfterApply)
    {
        if (extraShots <= 0)
            return false;

        int targetLimit = Clamp(Math.Max(_limit, limitAfterApply), 0, _hardLimit);
        return ExtraShots + extraShots <= targetLimit;
    }

    public int Add(int extraShots)
    {
        int accepted = Math.Min(Math.Max(0, extraShots), _limit - ExtraShots);
        ExtraShots += Math.Max(0, accepted);
        return accepted;
    }

    public void ExpandLimit(int limit)
    {
        _limit = Clamp(Math.Max(_limit, limit), 0, _hardLimit);
    }

    public SalvoProgressionState Clone()
    {
        return new SalvoProgressionState(_limit, _hardLimit, ExtraShots);
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        return Math.Max(minimum, Math.Min(maximum, value));
    }
}
