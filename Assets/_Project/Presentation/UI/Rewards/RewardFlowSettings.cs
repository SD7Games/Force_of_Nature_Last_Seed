using System;

public sealed class RewardFlowSettings
{
    public RewardFlowSettings(
        int freeRerollAttemptsPerSession,
        int adRerollAttemptsPerSession,
        int takeAllAttemptsPerSession)
    {
        FreeRerollAttemptsPerSession = Math.Max(0, freeRerollAttemptsPerSession);
        AdRerollAttemptsPerSession = Math.Max(0, adRerollAttemptsPerSession);
        TakeAllAttemptsPerSession = Math.Max(0, takeAllAttemptsPerSession);
    }

    public int FreeRerollAttemptsPerSession { get; }
    public int AdRerollAttemptsPerSession { get; }
    public int TakeAllAttemptsPerSession { get; }
}
