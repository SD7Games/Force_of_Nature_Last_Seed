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
    private readonly Action _onRerollClicked;
    private readonly Action _onAdRerollClicked;
    private readonly Action _onTakeAllClicked;
    private readonly RewardPopupActionStatePresenter _statePresenter;
    private readonly RewardPopupActionLayoutAnimator _layoutAnimator;

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
        _onRerollClicked = onRerollClicked;
        _onAdRerollClicked = onAdRerollClicked;
        _onTakeAllClicked = onTakeAllClicked;
        _statePresenter = new RewardPopupActionStatePresenter(
            rerollButton,
            adRerollButton,
            takeAllButton,
            rerollAttemptsText,
            adRerollAttemptsText,
            takeAllAttemptsText,
            guaranteeText,
            adRerollGuaranteeText,
            textSettings);
        _layoutAnimator = new RewardPopupActionLayoutAnimator(
            rerollButton,
            adRerollButton,
            takeAllButton,
            rerollAttemptsText,
            adRerollAttemptsText,
            takeAllAttemptsText,
            singleActionButtonAnchoredX);
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
        _statePresenter.Apply(state, interactable);
        _layoutAnimator.SetState(state);

        if (resetLayout)
            _layoutAnimator.ResetIfCached();
    }

    public void SetInteractable(bool enabled)
    {
        _statePresenter.SetInteractable(enabled);
    }

    public void PrepareForEnter(float yOffset)
    {
        _layoutAnimator.PrepareForEnter(yOffset);
    }

    public void InsertEnterTweens(
        Sequence sequence,
        float startTime,
        float duration)
    {
        _layoutAnimator.InsertEnterTweens(sequence, startTime, duration);
    }

    public void InsertExitTweens(
        Sequence sequence,
        float startTime,
        float yOffset,
        float duration,
        Ease exitEase)
    {
        _layoutAnimator.InsertExitTweens(sequence, startTime, yOffset, duration, exitEase);
    }

    public void ResetLayout()
    {
        _layoutAnimator.Reset();
    }

    public void KillAnimations()
    {
        _layoutAnimator.KillAnimations();
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
