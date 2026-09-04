using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

internal sealed class WormBalanceAdSessionState
{
    private int _freeRerollsLeft;
    private int _adRerollsLeft;
    private int _takeAllAdsLeft;
    private int _revivesLeft;

    private WormBalanceAdSessionState(
        int freeRerollsLeft,
        int adRerollsLeft,
        int takeAllAdsLeft,
        int revivesLeft)
    {
        _freeRerollsLeft = Mathf.Max(0, freeRerollsLeft);
        _adRerollsLeft = Mathf.Max(0, adRerollsLeft);
        _takeAllAdsLeft = Mathf.Max(0, takeAllAdsLeft);
        _revivesLeft = Mathf.Max(0, revivesLeft);
    }

    public int AdsWatched { get; private set; }
    public int FreeRerollsUsed { get; private set; }
    public int AdRerollsUsed { get; private set; }
    public int TakeAllAdsUsed { get; private set; }
    public int RevivesUsed { get; private set; }

    public static WormBalanceAdSessionState Create(
        WormBalanceSimulationSettings settings,
        WormBalanceScenario scenario)
    {
        bool allowPaidAssist = scenario is
            WormBalanceScenario.AdsAssistNoRevive or
            WormBalanceScenario.AdsAssist;
        bool allowRevive = scenario is
            WormBalanceScenario.ReviveOnly or
            WormBalanceScenario.AdsAssist;

        return new WormBalanceAdSessionState(
            settings.FreeRerollAttemptsPerSession,
            allowPaidAssist ? settings.AdRerollAttemptsPerSession : 0,
            allowPaidAssist ? settings.TakeAllAttemptsPerSession : 0,
            allowRevive ? settings.ReviveAttemptsPerSession : 0);
    }

    public bool TryUseFreeReroll()
    {
        if (_freeRerollsLeft <= 0)
            return false;

        _freeRerollsLeft--;
        FreeRerollsUsed++;
        return true;
    }

    public bool TryUseAdReroll()
    {
        if (_adRerollsLeft <= 0)
            return false;

        _adRerollsLeft--;
        AdRerollsUsed++;
        AdsWatched++;
        return true;
    }

    public bool TryUseTakeAll()
    {
        if (_takeAllAdsLeft <= 0)
            return false;

        _takeAllAdsLeft--;
        TakeAllAdsUsed++;
        AdsWatched++;
        return true;
    }

    public bool TryUseRevive()
    {
        if (_revivesLeft <= 0)
            return false;

        _revivesLeft--;
        RevivesUsed++;
        AdsWatched++;
        return true;
    }

    public WormBalanceAdSessionStats ToStats()
    {
        return new WormBalanceAdSessionStats(
            AdsWatched,
            FreeRerollsUsed,
            AdRerollsUsed,
            TakeAllAdsUsed,
            RevivesUsed);
    }
}
