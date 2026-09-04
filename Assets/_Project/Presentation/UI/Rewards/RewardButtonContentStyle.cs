using UnityEngine;

public readonly struct RewardButtonContentStyle
{
    public RewardButtonContentStyle(
        Color32 numberColor,
        Color32 titleColor,
        Color32 descriptionColor,
        Color32 valueColor,
        Color32 unlockTitleColor,
        Color32 unlockDescriptionColor,
        Color32 unlockValueColor,
        string unlockValueFallback)
    {
        NumberColor = numberColor;
        TitleColor = titleColor;
        DescriptionColor = descriptionColor;
        ValueColor = valueColor;
        UnlockTitleColor = unlockTitleColor;
        UnlockDescriptionColor = unlockDescriptionColor;
        UnlockValueColor = unlockValueColor;
        UnlockValueFallback = unlockValueFallback;
    }

    public Color32 NumberColor { get; }
    public Color32 TitleColor { get; }
    public Color32 DescriptionColor { get; }
    public Color32 ValueColor { get; }
    public Color32 UnlockTitleColor { get; }
    public Color32 UnlockDescriptionColor { get; }
    public Color32 UnlockValueColor { get; }
    public string UnlockValueFallback { get; }
}
