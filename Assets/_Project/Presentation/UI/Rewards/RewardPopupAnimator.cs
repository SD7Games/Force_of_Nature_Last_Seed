using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class RewardPopupAnimator
{
    private const float ShowSettleTimeSeconds = 0.28f;
    private const float CardRevealTimeSeconds = 0.14f;

    private readonly CanvasGroup _canvasGroup;
    private readonly RewardPopupChoiceBinder _choiceBinder;
    private readonly RewardPopupActionControls _actionControls;
    private readonly RewardPopupAnimationSettings _settings;
    private readonly RewardPopupAudioPlayer _audioPlayer;
    private readonly Action _onTransitionStarted;
    private readonly RewardPopupAnimatedLayout _animatedLayout;
    private readonly RewardPopupRefreshAnimationBuilder _refreshAnimationBuilder;

    private Sequence _showSequence;
    private Sequence _refreshSequence;
    private Sequence _dismissSequence;

    public RewardPopupAnimator(
        CanvasGroup canvasGroup,
        RectTransform[] topSlideGroups,
        RewardPopupChoiceBinder choiceBinder,
        RewardPopupActionControls actionControls,
        RewardPopupAnimationSettings settings,
        RewardPopupAudioPlayer audioPlayer,
        Action onTransitionStarted)
    {
        _canvasGroup = canvasGroup;
        _choiceBinder = choiceBinder;
        _actionControls = actionControls;
        _settings = settings;
        _audioPlayer = audioPlayer;
        _onTransitionStarted = onTransitionStarted;
        _animatedLayout = new RewardPopupAnimatedLayout(
            topSlideGroups,
            choiceBinder,
            actionControls,
            settings);
        _refreshAnimationBuilder = new RewardPopupRefreshAnimationBuilder(
            choiceBinder,
            settings,
            audioPlayer);
    }

    public bool IsTransitioning { get; private set; }

    public void PlayShow(Action onComplete)
    {
        BeginTransition();
        ResetAnimatedLayout();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
        }

        _animatedLayout.PrepareForShow();

        _showSequence?.Kill(false);
        _showSequence = DOTween.Sequence().SetUpdate(true);
        _showSequence.InsertCallback(0f, _audioPlayer.PlayShowWhoosh);

        if (_canvasGroup != null)
            _showSequence.Insert(0f, _canvasGroup.DOFade(1f, _settings.RootFadeDuration).SetEase(Ease.OutSine));

        _animatedLayout.InsertShowTweens(_showSequence);

        _showSequence.InsertCallback(ShowSettleTimeSeconds, _audioPlayer.PlayShowSettle);
        _showSequence.InsertCallback(CardRevealTimeSeconds, _audioPlayer.PlayCardReveal);
        _showSequence.OnComplete(() => CompleteTransition(onComplete));
    }

    public void PlayRewardRefresh(
        List<RewardChoiceData> choices,
        RewardPopupState state,
        Action<RewardPopupState> applyRefreshedState,
        Action onComplete)
    {
        BeginTransition();

        _refreshSequence?.Kill(false);
        _refreshSequence = DOTween.Sequence().SetUpdate(true);
        _refreshAnimationBuilder.Populate(
            _refreshSequence,
            choices,
            state,
            applyRefreshedState);
        _refreshSequence.OnComplete(() => CompleteTransition(onComplete));
    }

    public void PlaySelectionDismiss(
        RewardChoiceData selectedChoice,
        Action requestClose)
    {
        BeginTransition();
        KillActiveSequences();

        if (_canvasGroup != null)
            _canvasGroup.DOKill();

        RewardButtonView selectedButton = _choiceBinder.FindBoundButton(selectedChoice);

        if (selectedButton == null)
        {
            requestClose?.Invoke();
            return;
        }

        _dismissSequence = DOTween.Sequence().SetUpdate(true);

        float actionExitStart = 0f;
        float rewardExitStart = Mathf.Max(0f, _settings.ActionEnterDuration * 0.5f);
        float selectedExitStart = _animatedLayout.InsertRewardDismissTweens(
            _dismissSequence,
            selectedButton,
            rewardExitStart);
        float topExitStart = selectedExitStart;

        _actionControls.InsertExitTweens(
            _dismissSequence,
            actionExitStart,
            _settings.ActionExitOffset,
            _settings.ActionEnterDuration,
            _settings.UnselectedExitEase);
        _animatedLayout.InsertTopExitTweens(_dismissSequence, topExitStart);

        if (_canvasGroup != null)
        {
            float lastMotionEnd = Mathf.Max(
                selectedExitStart + _settings.SelectionExitDuration,
                topExitStart + _settings.TopEnterDuration);
            float rootFadeStart = Mathf.Max(0f, lastMotionEnd - _settings.RootFadeDuration);
            _dismissSequence.Insert(rootFadeStart, _canvasGroup.DOFade(0f, _settings.RootFadeDuration).SetEase(Ease.InSine));
        }

        _dismissSequence.OnComplete(() =>
        {
            _dismissSequence = null;
            requestClose?.Invoke();
        });
    }

    public void Stop()
    {
        KillAnimations();
        IsTransitioning = false;
    }

    public void ResetAnimatedLayout()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        _animatedLayout.Reset();
    }

    private void BeginTransition()
    {
        IsTransitioning = true;
        _onTransitionStarted?.Invoke();
    }

    private void CompleteTransition(Action onComplete)
    {
        IsTransitioning = false;
        ResetAnimatedLayout();
        onComplete?.Invoke();
    }

    private void KillAnimations()
    {
        if (_canvasGroup != null)
            _canvasGroup.DOKill();

        KillActiveSequences();

        _animatedLayout.KillAnimations();
    }

    private void KillActiveSequences()
    {
        _showSequence?.Kill(false);
        _showSequence = null;

        _refreshSequence?.Kill(false);
        _refreshSequence = null;

        _dismissSequence?.Kill(false);
        _dismissSequence = null;
    }

}
