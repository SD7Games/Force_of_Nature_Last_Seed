using System;

public sealed class DisabledRewardedAdService : IRewardedAdService
{
    public bool IsReady => false;

    public void ShowRewardedAd(Action<bool> onCompleted)
    {
        onCompleted?.Invoke(false);
    }
}
