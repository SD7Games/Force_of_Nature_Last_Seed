using System;

public sealed class RewardAdOperation
{
    private readonly IRewardedAdService _rewardedAdService;

    private int _version;

    public RewardAdOperation(IRewardedAdService rewardedAdService)
    {
        _rewardedAdService = rewardedAdService
            ?? throw new ArgumentNullException(nameof(rewardedAdService));
    }

    public bool IsPending { get; private set; }

    public bool TryBegin(Action<bool> onCompleted)
    {
        if (onCompleted == null)
            throw new ArgumentNullException(nameof(onCompleted));

        if (IsPending)
            return false;

        IsPending = true;
        int operationVersion = ++_version;
        _rewardedAdService.ShowRewardedAd(
            rewardGranted => Complete(operationVersion, onCompleted, rewardGranted));
        return true;
    }

    public void Cancel()
    {
        IsPending = false;
        _version++;
    }

    private void Complete(
        int operationVersion,
        Action<bool> onCompleted,
        bool rewardGranted)
    {
        if (!IsPending || operationVersion != _version)
            return;

        IsPending = false;
        onCompleted(rewardGranted);
    }
}
