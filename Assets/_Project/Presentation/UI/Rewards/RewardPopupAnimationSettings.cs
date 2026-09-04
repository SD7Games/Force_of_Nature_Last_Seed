using DG.Tweening;

public readonly struct RewardPopupAnimationSettings
{
    public RewardPopupAnimationSettings(
        float rootFadeDuration,
        float topEnterOffset,
        float topEnterDuration,
        float rewardEnterOffset,
        float rewardEnterDuration,
        float rewardEnterStagger,
        float actionEnterOffset,
        float actionEnterDuration,
        Ease topEnterEase,
        Ease rewardEnterEase,
        Ease rewardScaleEase,
        float refreshCardStagger,
        float refreshOutDuration,
        float refreshInDuration,
        Ease refreshOutEase,
        Ease refreshInEase,
        float selectionFocusDuration,
        float selectionGrowDuration,
        float selectionExitDuration,
        float selectionScaleMultiplier,
        float selectionExitScaleMultiplier,
        float selectionExitOffset,
        float unselectedExitDuration,
        float unselectedExitStagger,
        float unselectedExitScaleMultiplier,
        float unselectedExitOffset,
        float topExitOffset,
        float actionExitOffset,
        Ease selectionFocusEase,
        Ease selectionExitEase,
        Ease unselectedExitEase)
    {
        RootFadeDuration = rootFadeDuration;
        TopEnterOffset = topEnterOffset;
        TopEnterDuration = topEnterDuration;
        RewardEnterOffset = rewardEnterOffset;
        RewardEnterDuration = rewardEnterDuration;
        RewardEnterStagger = rewardEnterStagger;
        ActionEnterOffset = actionEnterOffset;
        ActionEnterDuration = actionEnterDuration;
        TopEnterEase = topEnterEase;
        RewardEnterEase = rewardEnterEase;
        RewardScaleEase = rewardScaleEase;
        RefreshCardStagger = refreshCardStagger;
        RefreshOutDuration = refreshOutDuration;
        RefreshInDuration = refreshInDuration;
        RefreshOutEase = refreshOutEase;
        RefreshInEase = refreshInEase;
        SelectionFocusDuration = selectionFocusDuration;
        SelectionGrowDuration = selectionGrowDuration;
        SelectionExitDuration = selectionExitDuration;
        SelectionScaleMultiplier = selectionScaleMultiplier;
        SelectionExitScaleMultiplier = selectionExitScaleMultiplier;
        SelectionExitOffset = selectionExitOffset;
        UnselectedExitDuration = unselectedExitDuration;
        UnselectedExitStagger = unselectedExitStagger;
        UnselectedExitScaleMultiplier = unselectedExitScaleMultiplier;
        UnselectedExitOffset = unselectedExitOffset;
        TopExitOffset = topExitOffset;
        ActionExitOffset = actionExitOffset;
        SelectionFocusEase = selectionFocusEase;
        SelectionExitEase = selectionExitEase;
        UnselectedExitEase = unselectedExitEase;
    }

    public float RootFadeDuration { get; }
    public float TopEnterOffset { get; }
    public float TopEnterDuration { get; }
    public float RewardEnterOffset { get; }
    public float RewardEnterDuration { get; }
    public float RewardEnterStagger { get; }
    public float ActionEnterOffset { get; }
    public float ActionEnterDuration { get; }
    public Ease TopEnterEase { get; }
    public Ease RewardEnterEase { get; }
    public Ease RewardScaleEase { get; }
    public float RefreshCardStagger { get; }
    public float RefreshOutDuration { get; }
    public float RefreshInDuration { get; }
    public Ease RefreshOutEase { get; }
    public Ease RefreshInEase { get; }
    public float SelectionFocusDuration { get; }
    public float SelectionGrowDuration { get; }
    public float SelectionExitDuration { get; }
    public float SelectionScaleMultiplier { get; }
    public float SelectionExitScaleMultiplier { get; }
    public float SelectionExitOffset { get; }
    public float UnselectedExitDuration { get; }
    public float UnselectedExitStagger { get; }
    public float UnselectedExitScaleMultiplier { get; }
    public float UnselectedExitOffset { get; }
    public float TopExitOffset { get; }
    public float ActionExitOffset { get; }
    public Ease SelectionFocusEase { get; }
    public Ease SelectionExitEase { get; }
    public Ease UnselectedExitEase { get; }
}
