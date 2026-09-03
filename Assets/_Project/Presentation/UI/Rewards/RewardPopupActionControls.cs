using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RewardPopupActionControls
{
    private readonly Button _rerollButton;
    private readonly Button _adRerollButton;
    private readonly Button _takeAllButton;
    private readonly TMP_Text _rerollAttemptsText;
    private readonly TMP_Text _adRerollAttemptsText;
    private readonly TMP_Text _takeAllAttemptsText;
    private readonly TMP_Text _guaranteeText;
    private readonly TMP_Text _adRerollGuaranteeText;
    private readonly TextSettings _textSettings;
    private readonly float _singleActionButtonAnchoredX;
    private readonly Action _onRerollClicked;
    private readonly Action _onAdRerollClicked;
    private readonly Action _onTakeAllClicked;

    private RewardPopupRectTransformState[] _buttonStates;
    private RewardPopupRectTransformState _rerollAttemptsTextState;
    private RewardPopupRectTransformState _adRerollAttemptsTextState;
    private RewardPopupRectTransformState _takeAllAttemptsTextState;
    private RewardPopupState _currentState;
    private bool _hasCurrentState;
    private bool _hasCachedLayoutState;

    public RewardPopupActionControls(
        Button rerollButton,
        Button adRerollButton,
        Button takeAllButton,
        TMP_Text rerollAttemptsText,
        TMP_Text adRerollAttemptsText,
        TMP_Text takeAllAttemptsText,
        TMP_Text guaranteeText,
        TMP_Text adRerollGuaranteeText,
        TextSettings textSettings,
        float singleActionButtonAnchoredX,
        Action onRerollClicked,
        Action onAdRerollClicked,
        Action onTakeAllClicked)
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
        _singleActionButtonAnchoredX = singleActionButtonAnchoredX;
        _onRerollClicked = onRerollClicked;
        _onAdRerollClicked = onAdRerollClicked;
        _onTakeAllClicked = onTakeAllClicked;
    }

    public void Subscribe()
    {
        Unsubscribe();

        if (_rerollButton != null)
            _rerollButton.onClick.AddListener(HandleRerollClicked);

        if (_adRerollButton != null)
            _adRerollButton.onClick.AddListener(HandleAdRerollClicked);

        if (_takeAllButton != null)
            _takeAllButton.onClick.AddListener(HandleTakeAllClicked);
    }

    public void Unsubscribe()
    {
        if (_rerollButton != null)
            _rerollButton.onClick.RemoveListener(HandleRerollClicked);

        if (_adRerollButton != null)
            _adRerollButton.onClick.RemoveListener(HandleAdRerollClicked);

        if (_takeAllButton != null)
            _takeAllButton.onClick.RemoveListener(HandleTakeAllClicked);
    }

    public void ApplyState(
        RewardPopupState state,
        bool interactable,
        bool resetLayout)
    {
        _currentState = state;
        _hasCurrentState = true;

        ApplyRerollButtonMode(state);
        ApplyTakeAllButtonMode(state);
        ApplyAttemptsText(_rerollAttemptsText, state.FreeRerollAttemptsLeft);
        ApplyAttemptsText(_adRerollAttemptsText, state.AdRerollAttemptsLeft);
        ApplyAttemptsText(_takeAllAttemptsText, state.TakeAllAttemptsLeft);

        ApplyGuaranteeText(_guaranteeText, state.GuaranteeRarity);
        ApplyAdGuaranteeText(_adRerollGuaranteeText, state.AdRerollGuaranteeRarity);

        SetInteractable(interactable);

        if (resetLayout)
            ResetLayoutIfCached();
    }

    public void SetInteractable(bool enabled)
    {
        bool hasState = _hasCurrentState;

        if (_rerollButton != null)
            _rerollButton.interactable = enabled && hasState && _currentState.CanFreeReroll;

        if (_adRerollButton != null)
            _adRerollButton.interactable = enabled && hasState && _currentState.CanAdReroll;

        if (_takeAllButton != null)
            _takeAllButton.interactable = enabled && hasState && _currentState.CanTakeAll;
    }

    public void PrepareForEnter(float yOffset)
    {
        EnsureLayoutCached();

        for (int i = 0; i < _buttonStates.Length; i++)
        {
            _buttonStates[i].Kill();
            _buttonStates[i].Prepare(
                GetActionButtonTargetPosition(_buttonStates[i]),
                yOffset,
                0.98f);
        }
    }

    public void InsertEnterTweens(
        Sequence sequence,
        float startTime,
        float duration)
    {
        EnsureLayoutCached();

        int activeIndex = 0;

        for (int i = 0; i < _buttonStates.Length; i++)
        {
            if (!_buttonStates[i].IsActive)
                continue;

            sequence.Insert(
                startTime + activeIndex * 0.035f,
                _buttonStates[i].CreateEnterTween(
                    GetActionButtonTargetPosition(_buttonStates[i]),
                    duration,
                    Ease.OutCubic,
                    Ease.OutBack));
            activeIndex++;
        }
    }

    public void InsertExitTweens(
        Sequence sequence,
        float startTime,
        float yOffset,
        float duration,
        Ease exitEase)
    {
        EnsureLayoutCached();

        int activeIndex = 0;

        for (int i = _buttonStates.Length - 1; i >= 0; i--)
        {
            if (!_buttonStates[i].IsActive)
                continue;

            sequence.Insert(
                startTime + activeIndex * 0.035f,
                _buttonStates[i].CreateExitTween(
                    GetActionButtonTargetPosition(_buttonStates[i]),
                    yOffset,
                    0.98f,
                    duration,
                    exitEase,
                    exitEase));
            activeIndex++;
        }
    }

    public void ResetLayout()
    {
        EnsureLayoutCached();
        ResetCachedLayout();
    }

    private void ResetLayoutIfCached()
    {
        if (!_hasCachedLayoutState)
            return;

        ResetCachedLayout();
    }

    private void ResetCachedLayout()
    {
        for (int i = 0; i < _buttonStates.Length; i++)
            _buttonStates[i].Reset(GetActionButtonTargetPosition(_buttonStates[i]));

        _rerollAttemptsTextState.Reset();
        _adRerollAttemptsTextState.Reset();
        _takeAllAttemptsTextState.Reset();
    }

    public void KillAnimations()
    {
        if (_buttonStates == null)
            return;

        for (int i = 0; i < _buttonStates.Length; i++)
            _buttonStates[i].Kill();
    }

    private void EnsureLayoutCached()
    {
        if (_hasCachedLayoutState)
            return;

        _buttonStates = CreateButtonStates();
        _rerollAttemptsTextState = new RewardPopupRectTransformState(GetRectTransform(_rerollAttemptsText));
        _adRerollAttemptsTextState = new RewardPopupRectTransformState(GetRectTransform(_adRerollAttemptsText));
        _takeAllAttemptsTextState = new RewardPopupRectTransformState(GetRectTransform(_takeAllAttemptsText));
        _hasCachedLayoutState = true;
    }

    private RewardPopupRectTransformState[] CreateButtonStates()
    {
        RectTransform reroll = GetRectTransform(_rerollButton);
        RectTransform adReroll = GetRectTransform(_adRerollButton);
        RectTransform takeAll = GetRectTransform(_takeAllButton);

        int count = 0;

        if (reroll != null)
            count++;

        if (adReroll != null)
            count++;

        if (takeAll != null)
            count++;

        RewardPopupRectTransformState[] states = new RewardPopupRectTransformState[count];
        int index = 0;

        if (reroll != null)
            states[index++] = new RewardPopupRectTransformState(reroll);

        if (adReroll != null)
            states[index++] = new RewardPopupRectTransformState(adReroll);

        if (takeAll != null)
            states[index] = new RewardPopupRectTransformState(takeAll);

        return states;
    }

    private Vector2 GetActionButtonTargetPosition(RewardPopupRectTransformState state)
    {
        if (!ShouldUseSingleActionLayout())
            return state.BaseAnchoredPosition;

        RectTransform activeReroll = _currentState.UseFreeRerollButton
            ? GetRectTransform(_rerollButton)
            : GetRectTransform(_adRerollButton);

        if (state.RectTransform != activeReroll)
            return state.BaseAnchoredPosition;

        return new Vector2(
            _singleActionButtonAnchoredX,
            state.BaseAnchoredPosition.y);
    }

    private bool ShouldUseSingleActionLayout()
    {
        return _hasCurrentState && !_currentState.UseTakeAllButton;
    }

    private void ApplyRerollButtonMode(RewardPopupState state)
    {
        bool showFreeReroll = state.UseFreeRerollButton;

        if (_rerollButton != null)
            _rerollButton.gameObject.SetActive(showFreeReroll);

        if (_adRerollButton != null)
            _adRerollButton.gameObject.SetActive(!showFreeReroll);
    }

    private void ApplyTakeAllButtonMode(RewardPopupState state)
    {
        bool showTakeAll = state.UseTakeAllButton;

        if (_takeAllButton != null)
            _takeAllButton.gameObject.SetActive(showTakeAll);

        if (_takeAllAttemptsText != null)
            _takeAllAttemptsText.gameObject.SetActive(showTakeAll);
    }

    private void ApplyGuaranteeText(TMP_Text text, RewardRarity rarity)
    {
        if (text == null)
            return;

        text.text = RewardTextFormatter.FormatRarityLine(
            _textSettings.GuaranteeFormat,
            rarity,
            _textSettings.CommonRarityColor,
            _textSettings.RareRarityColor,
            _textSettings.LegendaryRarityColor);
    }

    private void ApplyAdGuaranteeText(TMP_Text text, RewardRarity rarity)
    {
        if (text == null)
            return;

        text.text = RewardTextFormatter.FormatRarityLine(
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

    private void HandleRerollClicked()
    {
        _onRerollClicked?.Invoke();
    }

    private void HandleAdRerollClicked()
    {
        _onAdRerollClicked?.Invoke();
    }

    private void HandleTakeAllClicked()
    {
        _onTakeAllClicked?.Invoke();
    }

    private static RectTransform GetRectTransform(Button button)
    {
        return button != null
            ? button.transform as RectTransform
            : null;
    }

    private static RectTransform GetRectTransform(TMP_Text text)
    {
        return text != null
            ? text.transform as RectTransform
            : null;
    }

    public readonly struct TextSettings
    {
        public TextSettings(
            string attemptsFormat,
            string guaranteeFormat,
            string adGuaranteeFormat,
            Color32 numberColor,
            Color32 commonRarityColor,
            Color32 rareRarityColor,
            Color32 legendaryRarityColor)
        {
            AttemptsFormat = attemptsFormat;
            GuaranteeFormat = guaranteeFormat;
            AdGuaranteeFormat = adGuaranteeFormat;
            NumberColor = numberColor;
            CommonRarityColor = commonRarityColor;
            RareRarityColor = rareRarityColor;
            LegendaryRarityColor = legendaryRarityColor;
        }

        public string AttemptsFormat { get; }
        public string GuaranteeFormat { get; }
        public string AdGuaranteeFormat { get; }
        public Color32 NumberColor { get; }
        public Color32 CommonRarityColor { get; }
        public Color32 RareRarityColor { get; }
        public Color32 LegendaryRarityColor { get; }
    }
}
