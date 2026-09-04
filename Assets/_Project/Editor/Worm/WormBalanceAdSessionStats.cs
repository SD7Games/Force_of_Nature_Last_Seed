using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

internal readonly struct WormBalanceAdSessionStats
{
    public readonly int AdsWatched;
    public readonly int FreeRerollsUsed;
    public readonly int AdRerollsUsed;
    public readonly int TakeAllAdsUsed;
    public readonly int RevivesUsed;

    public WormBalanceAdSessionStats(
        int adsWatched,
        int freeRerollsUsed,
        int adRerollsUsed,
        int takeAllAdsUsed,
        int revivesUsed)
    {
        AdsWatched = Mathf.Max(0, adsWatched);
        FreeRerollsUsed = Mathf.Max(0, freeRerollsUsed);
        AdRerollsUsed = Mathf.Max(0, adRerollsUsed);
        TakeAllAdsUsed = Mathf.Max(0, takeAllAdsUsed);
        RevivesUsed = Mathf.Max(0, revivesUsed);
    }
}
