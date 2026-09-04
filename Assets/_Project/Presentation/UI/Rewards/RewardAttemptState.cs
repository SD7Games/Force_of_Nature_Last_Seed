public sealed class RewardAttemptState
{
    private readonly RewardFlowSettings _settings;
    private int _freeRerollsLeft;
    private int _adRerollsLeft;
    private int _takeAllLeft;

    public RewardAttemptState(RewardFlowSettings settings)
    {
        _settings = settings
            ?? throw new System.ArgumentNullException(nameof(settings));
        Reset();
    }

    public int FreeRerollsLeft => _freeRerollsLeft;
    public int AdRerollsLeft => _adRerollsLeft;
    public int TakeAllLeft => _takeAllLeft;

    public bool HasFreeReroll => FreeRerollsLeft > 0;
    public bool HasAdReroll => AdRerollsLeft > 0;
    public bool HasTakeAll => TakeAllLeft > 0;

    public bool ConsumeFreeReroll()
    {
        return TryConsume(ref _freeRerollsLeft);
    }

    public bool ConsumeAdReroll()
    {
        return TryConsume(ref _adRerollsLeft);
    }

    public bool ConsumeTakeAll()
    {
        return TryConsume(ref _takeAllLeft);
    }

    public void Reset()
    {
        _freeRerollsLeft = _settings.FreeRerollAttemptsPerSession;
        _adRerollsLeft = _settings.AdRerollAttemptsPerSession;
        _takeAllLeft = _settings.TakeAllAttemptsPerSession;
    }

    private static bool TryConsume(ref int attempts)
    {
        if (attempts <= 0)
            return false;

        attempts--;
        return true;
    }
}
