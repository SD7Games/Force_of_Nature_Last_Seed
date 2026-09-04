using System;

public interface IRewardedAdService
{
    bool IsReady { get; }

    void ShowRewardedAd(Action<bool> onCompleted);
}
