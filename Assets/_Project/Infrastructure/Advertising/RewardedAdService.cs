using System;
using UnityEngine;

public abstract class RewardedAdService : MonoBehaviour, IRewardedAdService
{
    public abstract bool IsReady { get; }

    public abstract void ShowRewardedAd(Action<bool> onCompleted);
}
