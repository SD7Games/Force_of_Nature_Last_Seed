using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class RewardPopupActionStatePresenter
{
    private readonly Button _rerollButton;
    private readonly Button _adRerollButton;
    private readonly Button _takeAllButton;
    private readonly TMP_Text _rerollAttemptsText;
    private readonly TMP_Text _adRerollAttemptsText;
    private readonly TMP_Text _takeAllAttemptsText;
    private readonly TMP_Text _guaranteeText;
    private readonly TMP_Text _adRerollGuaranteeText;
    private readonly RewardPopupActionControls.TextSettings _textSettings;

    private RewardPopupState _currentState;
    private bool _hasCurrentState;

    public RewardPopupActionStatePresenter(
        Button rerollButton,
        Button adRerollButton,
        Button takeAllButton,
        TMP_Text rerollAttemptsText,
        TMP_Text adRerollAttemptsText,
        TMP_Text takeAllAttemptsText,
        TMP_Text guaranteeText,
        TMP_Text adRerollGuaranteeText,
        RewardPopupActionControls.TextSettings textSettings)
    {
        _rerollButton = rerollButton;
        _adRerollButton = adRerollButton;
        _takeAllButton = takeAllButton;
        _rerollAttemptsText = rerollAttemptsText;
        _adRerollAttemptsText = adRerollAttemptsText;
        _takeAllAttemptsText = takeAllAttemptsText;
        _guaranteeText = guaranteeText;
        _adRerollGuaranteeText = adRerollGuaranteeText;
        _textSettings = textSettings;
    }

    public void Apply(RewardPopupState state, bool interactable)
    {
        _currentState = state;
        _hasCurrentState = true;
        ApplyButtonModes(state);
        ApplyAttemptsText(_rerollAttemptsText, state.FreeRerollAttemptsLeft);
        ApplyAttemptsText(_adRerollAttemptsText, state.AdRerollAttemptsLeft);
        ApplyAttemptsText(_takeAllAttemptsText, state.TakeAllAttemptsLeft);
        ApplyGuaranteeText(state.GuaranteeRarity);
        ApplyAdGuaranteeText(state.AdRerollGuaranteeRarity);
        SetInteractable(interactable);
    }

    public void SetInteractable(bool enabled)
    {
        if (_rerollButton != null)
            _rerollButton.interactable = enabled && _hasCurrentState && _currentState.CanFreeReroll;

        if (_adRerollButton != null)
            _adRerollButton.interactable = enabled && _hasCurrentState && _currentState.CanAdReroll;

        if (_takeAllButton != null)
            _takeAllButton.interactable = enabled && _hasCurrentState && _currentState.CanTakeAll;
    }

    private void ApplyButtonModes(RewardPopupState state)
    {
        if (_rerollButton != null)
            _rerollButton.gameObject.SetActive(state.UseFreeRerollButton);

        if (_adRerollButton != null)
            _adRerollButton.gameObject.SetActive(!state.UseFreeRerollButton);

        if (_takeAllButton != null)
            _takeAllButton.gameObject.SetActive(state.UseTakeAllButton);

        if (_takeAllAttemptsText != null)
            _takeAllAttemptsText.gameObject.SetActive(state.UseTakeAllButton);
    }

    private void ApplyGuaranteeText(RewardRarity rarity)
    {
        if (_guaranteeText == null)
            return;

        _guaranteeText.text = RewardTextFormatter.FormatRarityLine(
            _textSettings.GuaranteeFormat,
            rarity,
            _textSettings.CommonRarityColor,
            _textSettings.RareRarityColor,
            _textSettings.LegendaryRarityColor);
    }

    private void ApplyAdGuaranteeText(RewardRarity rarity)
    {
        if (_adRerollGuaranteeText == null)
            return;

        _adRerollGuaranteeText.text = RewardTextFormatter.FormatRarityLine(
            _textSettings.AdGuaranteeFormat,
            rarity,
            _textSettings.CommonRarityColor,
            _textSettings.RareRarityColor,
            _textSettings.LegendaryRarityColor,
            _textSettings.NumberColor);
    }

    private void ApplyAttemptsText(TMP_Text text, int attemptsLeft)
    {
        if (text == null)
            return;

        string value = string.Format(_textSettings.AttemptsFormat, Mathf.Max(0, attemptsLeft));
        text.text = RewardTextFormatter.HighlightAttempts(value, _textSettings.NumberColor);
    }
}
