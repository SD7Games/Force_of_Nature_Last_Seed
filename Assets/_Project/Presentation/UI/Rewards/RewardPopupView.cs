using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays reward choices and routes popup UI events.
/// </summary>
[DisallowMultipleComponent]
public sealed class RewardPopupView : PopupView
{
    [SerializeField] private List<RewardButtonView> _buttons;
    [SerializeField] private RewardVisualCatalog _visualCatalog;
    [SerializeField] private Button _rerollButton;
    [SerializeField] private Button _adRerollButton;
    [SerializeField] private Button _takeAllButton;

    [Header("Popup Animation")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform[] _topSlideGroups;
    [SerializeField] private float _rootFadeDuration = 0.12f;
    [SerializeField] private float _topEnterOffset = 160f;
    [SerializeField] private float _topEnterDuration = 0.28f;
    [SerializeField] private float _rewardEnterOffset = -230f;
    [SerializeField] private float _rewardEnterDuration = 0.32f;
    [SerializeField] private float _rewardEnterStagger = 0.045f;
    [SerializeField] private float _actionEnterOffset = -90f;
    [SerializeField] private float _actionEnterDuration = 0.16f;
    [SerializeField] private Ease _topEnterEase = Ease.OutCubic;
    [SerializeField] private Ease _rewardEnterEase = Ease.OutCubic;
    [SerializeField] private Ease _rewardScaleEase = Ease.OutBack;

    [Header("Reward Refresh Animation")]
    [SerializeField] private float _refreshCardStagger = 0.055f;
    [SerializeField] private float _refreshOutDuration = 0.12f;
    [SerializeField] private float _refreshInDuration = 0.22f;
    [SerializeField] private Ease _refreshOutEase = Ease.InCubic;
    [SerializeField] private Ease _refreshInEase = Ease.OutBack;

    [Header("Selection Dismiss Animation")]
    [SerializeField] private float _selectionFocusDuration = 0.22f;
    [SerializeField] private float _selectionGrowDuration = 0.16f;
    [SerializeField] private float _selectionExitDuration = 0.22f;
    [SerializeField] private float _selectionScaleMultiplier = 1.05f;
    [SerializeField] private float _selectionExitScaleMultiplier = 0.96f;
    [SerializeField] private float _selectionExitOffset = -230f;
    [SerializeField] private float _unselectedExitDuration = 0.22f;
    [SerializeField] private float _unselectedExitStagger = 0.045f;
    [SerializeField] private float _unselectedExitScaleMultiplier = 0.96f;
    [SerializeField] private float _unselectedExitOffset = -230f;
    [SerializeField] private float _topExitOffset = 160f;
    [SerializeField] private float _actionExitOffset = -90f;
    [SerializeField] private Ease _selectionFocusEase = Ease.OutBack;
    [SerializeField] private Ease _selectionExitEase = Ease.InCubic;
    [SerializeField] private Ease _unselectedExitEase = Ease.InCubic;

    [Header("Animation Audio")]
    [SerializeField] private AudioSource _animationAudioSource;
    [SerializeField] private AudioClip _showWhooshClip;
    [SerializeField] private AudioClip _showSettleClip;
    [SerializeField] private AudioClip _refreshClip;
    [SerializeField] private AudioClip _cardRevealClip;
    [SerializeField, Range(0f, 1f)] private float _animationVolume = 1f;

    [Header("Action State Text")]
    [SerializeField] private TMP_Text _rerollAttemptsText;
    [SerializeField] private TMP_Text _adRerollAttemptsText;
    [SerializeField] private TMP_Text _takeAllAttemptsText;
    [SerializeField] private TMP_Text _guaranteeText;
    [SerializeField] private TMP_Text _adRerollGuaranteeText;
    [SerializeField] private string _attemptsFormat = "attempts left: x{0}";
    [SerializeField] private string _guaranteeFormat = "guarantee: {0}";
    [SerializeField] private string _adGuaranteeFormat = "guarantee: {0}";

    [Header("Action Layout")]
    [SerializeField] private float _singleActionButtonAnchoredX = 350f;

    [Header("Text Colors")]
    [SerializeField] private Color32 _numberColor = new(105, 255, 120, 255);
    [SerializeField] private Color32 _commonRarityColor = new(95, 220, 130, 255);
    [SerializeField] private Color32 _rareRarityColor = new(80, 180, 255, 255);
    [SerializeField] private Color32 _legendaryRarityColor = new(255, 155, 70, 255);

    public event Action<RewardChoiceData> Selected;
    public event Action RerollRequested;
    public event Action AdRerollRequested;
    public event Action TakeAllRequested;

    private RewardPopupChoiceBinder _choiceBinder;
    private RewardPopupActionControls _actionControls;
    private RewardPopupAnimator _animator;
    private RewardPopupInteractionGate _interactionGate;
    private bool _hasBoundChoices;

    private bool CanAcceptInteraction =>
        _interactionGate != null
        && _interactionGate.IsOpen
        && (_animator == null || !_animator.IsTransitioning);

    private void Awake()
    {
        EnsureControllers();
    }

    private void OnEnable()
    {
        EnsureControllers();
        _actionControls.Subscribe();
    }

    private void OnDisable()
    {
        _interactionGate?.Stop();
        _animator?.Stop();
        _actionControls?.Unsubscribe();
    }

    public bool Bind(
        List<RewardChoiceData> choices,
        RewardPopupState state,
        bool animateChoiceChanges = false)
    {
        EnsureControllers();

        if (choices == null || choices.Count == 0 || !_choiceBinder.HasBindableButtons)
        {
            Debug.LogWarning("RewardPopupView: reward choices or buttons are not assigned.", this);
            RequestClose();
            return false;
        }

        bool shouldAnimateRefresh = animateChoiceChanges && IsVisible && _hasBoundChoices;

        if (shouldAnimateRefresh)
        {
            _animator.PlayRewardRefresh(
                choices,
                state,
                ApplyRefreshedState,
                StartInteractionGateWhenSafe);
            return true;
        }

        _choiceBinder.ApplyChoices(choices, false);
        ApplyState(state, false, true);
        _hasBoundChoices = true;

        if (IsVisible)
            StartInteractionGateWhenSafe();

        return true;
    }

    public void Close()
    {
        RequestClose();
    }

    public void SetAllButtonsInteractable(bool interactable)
    {
        EnsureControllers();

        if (!interactable)
        {
            CloseInteractionGate();
            return;
        }

        StartInteractionGateWhenSafe();
    }

    protected override void OnShown()
    {
        EnsureControllers();
        _animator.PlayShow(StartInteractionGateWhenSafe);
    }

    protected override void OnHidden()
    {
        CloseInteractionGate();
        _animator?.Stop();
        _animator?.ResetAnimatedLayout();
        _hasBoundChoices = false;
    }

    private void EnsureControllers()
    {
        if (_choiceBinder != null)
            return;

        _choiceBinder = new RewardPopupChoiceBinder(
            _buttons,
            _visualCatalog,
            OnClicked);

        _actionControls = new RewardPopupActionControls(
            _rerollButton,
            _adRerollButton,
            _takeAllButton,
            _rerollAttemptsText,
            _adRerollAttemptsText,
            _takeAllAttemptsText,
            _guaranteeText,
            _adRerollGuaranteeText,
            new RewardPopupActionControls.TextSettings(
                _attemptsFormat,
                _guaranteeFormat,
                _adGuaranteeFormat,
                _numberColor,
                _commonRarityColor,
                _rareRarityColor,
                _legendaryRarityColor),
            _singleActionButtonAnchoredX,
            OnRerollClicked,
            OnAdRerollClicked,
            OnTakeAllClicked);

        _animator = new RewardPopupAnimator(
            _canvasGroup,
            _topSlideGroups,
            _choiceBinder,
            _actionControls,
            BuildAnimationSettings(),
            new RewardPopupAudioPlayer(
                _animationAudioSource,
                _showWhooshClip,
                _showSettleClip,
                _refreshClip,
                _cardRevealClip,
                _animationVolume),
            CloseInteractionGate);

        _interactionGate = new RewardPopupInteractionGate(
            this,
            CanOpenInteractionGate,
            SetInteractionEnabled);
    }

    private RewardPopupAnimationSettings BuildAnimationSettings()
    {
        return new RewardPopupAnimationSettings(
            _rootFadeDuration,
            _topEnterOffset,
            _topEnterDuration,
            _rewardEnterOffset,
            _rewardEnterDuration,
            _rewardEnterStagger,
            _actionEnterOffset,
            _actionEnterDuration,
            _topEnterEase,
            _rewardEnterEase,
            _rewardScaleEase,
            _refreshCardStagger,
            _refreshOutDuration,
            _refreshInDuration,
            _refreshOutEase,
            _refreshInEase,
            _selectionFocusDuration,
            _selectionGrowDuration,
            _selectionExitDuration,
            _selectionScaleMultiplier,
            _selectionExitScaleMultiplier,
            _selectionExitOffset,
            _unselectedExitDuration,
            _unselectedExitStagger,
            _unselectedExitScaleMultiplier,
            _unselectedExitOffset,
            _topExitOffset,
            _actionExitOffset,
            _selectionFocusEase,
            _selectionExitEase,
            _unselectedExitEase);
    }

    private bool CanOpenInteractionGate()
    {
        return isActiveAndEnabled && (_animator == null || !_animator.IsTransitioning);
    }

    private void ApplyRefreshedState(RewardPopupState state)
    {
        ApplyState(state, false, false);
        _hasBoundChoices = true;
    }

    private void ApplyState(
        RewardPopupState state,
        bool interactable,
        bool resetActionLayout)
    {
        _actionControls.ApplyState(state, interactable, resetActionLayout);
    }

    private void SetInteractionEnabled(bool enabled)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = enabled;
        }

        _choiceBinder.SetInteractable(enabled);
        _actionControls.SetInteractable(enabled);
    }

    private void CloseInteractionGate()
    {
        _interactionGate?.Close();
    }

    private void StartInteractionGateWhenSafe()
    {
        _interactionGate?.StartWhenSafe();
    }

    private void OnClicked(RewardChoiceData data)
    {
        if (!CanAcceptInteraction)
            return;

        CloseInteractionGate();
        Selected?.Invoke(data);
        _animator.PlaySelectionDismiss(data, RequestClose);
    }

    private void OnRerollClicked()
    {
        if (!CanAcceptInteraction)
            return;

        CloseInteractionGate();
        RerollRequested?.Invoke();
    }

    private void OnTakeAllClicked()
    {
        if (!CanAcceptInteraction)
            return;

        CloseInteractionGate();
        TakeAllRequested?.Invoke();
    }

    private void OnAdRerollClicked()
    {
        if (!CanAcceptInteraction)
            return;

        CloseInteractionGate();
        AdRerollRequested?.Invoke();
    }
}
