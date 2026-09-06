using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RewardButtonContentPresenter
{
    private readonly Image _icon;
    private readonly TMP_Text _title;
    private readonly TMP_Text _description;
    private readonly TMP_Text _value;
    private readonly Image _commonVisual;
    private readonly Image _rareVisual;
    private readonly Image _legendaryVisual;
    private readonly Image _unlockVisual;
    private readonly RewardButtonContentStyle _style;
    private readonly RewardTextFormatter _textFormatter = new();

    public RewardButtonContentPresenter(
        Image icon,
        TMP_Text title,
        TMP_Text description,
        TMP_Text value,
        Image commonVisual,
        Image rareVisual,
        Image legendaryVisual,
        Image unlockVisual,
        in RewardButtonContentStyle style)
    {
        _icon = icon;
        _title = title;
        _description = description;
        _value = value;
        _commonVisual = commonVisual;
        _rareVisual = rareVisual;
        _legendaryVisual = legendaryVisual;
        _unlockVisual = unlockVisual;
        _style = style;
    }

    public void Apply(RewardChoiceData data, RewardPresentationData presentation)
    {
        bool isUnlock = presentation.Kind == RewardPresentationKind.WeaponUnlock;
        ApplyColors(isUnlock);
        SetText(_title, data.Title);
        SetOptionalText(_description, data.Description);
        SetValue(data.ValueText, isUnlock);
        ApplyIcon(presentation.IconProfile);
        SetVisualActive(_commonVisual, !isUnlock && data.Rarity == RewardRarity.Common);
        SetVisualActive(_rareVisual, !isUnlock && data.Rarity == RewardRarity.Rare);
        SetVisualActive(_legendaryVisual, !isUnlock && data.Rarity == RewardRarity.Legendary);
        SetVisualActive(_unlockVisual, isUnlock);
    }

    private void ApplyColors(bool isUnlock)
    {
        SetColor(_title, isUnlock ? _style.UnlockTitleColor : _style.TitleColor);
        SetColor(
            _description,
            isUnlock ? _style.UnlockDescriptionColor : _style.DescriptionColor);
        SetColor(_value, isUnlock ? _style.UnlockValueColor : _style.ValueColor);
    }

    private void SetValue(string value, bool isUnlock)
    {
        if (_value == null)
            return;

        _value.text = isUnlock
            ? string.IsNullOrWhiteSpace(value) ? _style.UnlockValueFallback : value
            : _textFormatter.HighlightNumbers(value, _style.NumberColor);
    }

    private void ApplyIcon(RewardIconProfile iconProfile)
    {
        if (_icon == null)
            return;

        if (iconProfile == null || iconProfile.Sprite == null)
        {
            _icon.enabled = false;
            return;
        }

        iconProfile.ApplyTo(_icon);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }

    private static void SetOptionalText(TMP_Text text, string value)
    {
        if (text == null)
            return;

        bool hasValue = !string.IsNullOrWhiteSpace(value);
        text.gameObject.SetActive(hasValue);
        text.text = hasValue ? value : string.Empty;
    }

    private static void SetColor(TMP_Text text, Color32 color)
    {
        if (text != null)
            text.color = color;
    }

    private static void SetVisualActive(Image image, bool active)
    {
        if (image != null)
            image.gameObject.SetActive(active);
    }
}
