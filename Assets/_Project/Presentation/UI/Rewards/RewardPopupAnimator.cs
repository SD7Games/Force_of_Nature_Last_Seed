using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class RewardPopupAnimator
{
    private readonly CanvasGroup _canvasGroup;
    private readonly RectTransform[] _topSlideGroups;
    private readonly RewardPopupChoiceBinder _choiceBinder;
    private readonly RewardPopupActionControls _actionControls;
    private readonly RewardPopupAnimationSettings _settings;
    private readonly RewardPopupAudioPlayer _audioPlayer;
    private readonly Action _onTransitionStarted;

    private RewardPopupRectTransformState[] _topGroupStates;
    private Sequence _showSequence;
    private Sequence _refreshSequence;
    private Sequence _dismissSequence;
    private bool _hasCachedAnimationState;

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
        _topSlideGroups = topSlideGroups;
        _choiceBinder = choiceBinder;
        _actionControls = actionControls;
        _settings = settings;
        _audioPlayer = audioPlayer;
        _onTransitionStarted = onTransitionStarted;
    }

    public bool IsTransitioning { get; private set; }

    public void PlayShow(Action onComplete)
    {
        EnsureAnimationStateCached();
        BeginTransition();
        ResetAnimatedLayout();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
        }

        PrepareTopGroupsForEnter();
        PrepareRewardButtonsForEnter();
        _actionControls.PrepareForEnter(_settings.ActionEnterOffset);

        _showSequence?.Kill(false);
        _showSequence = DOTween.Sequence().SetUpdate(true);
        _showSequence.InsertCallback(0f, _audioPlayer.PlayShowWhoosh);

        if (_canvasGroup != null)
            _showSequence.Insert(0f, _canvasGroup.DOFade(1f, _settings.RootFadeDuration).SetEase(Ease.OutSine));

        InsertTopEnterTweens(_showSequence, 0.02f);
        InsertRewardEnterTweens(_showSequence, 0.1f);
        _actionControls.InsertEnterTweens(_showSequence, 0.3f, _settings.ActionEnterDuration);

        _showSequence.InsertCallback(0.28f, _audioPlayer.PlayShowSettle);
        _showSequence.InsertCallback(0.14f, _audioPlayer.PlayCardReveal);
        _showSequence.OnComplete(() => CompleteTransition(onComplete));
    }

    public void PlayRewardRefresh(
        List<RewardChoiceData> choices,
        RewardPopupState state,
        Action<RewardPopupState> applyRefreshedState,
        Action onComplete)
    {
        EnsureAnimationStateCached();
        BeginTransition();

        _refreshSequence?.Kill(false);
        _refreshSequence = DOTween.Sequence().SetUpdate(true);
        _refreshSequence.InsertCallback(0f, _audioPlayer.PlayRefresh);

        int choiceCount = choices != null ? choices.Count : 0;
        float lastDelay = 0f;

        for (int i = 0; i < _choiceBinder.ButtonCount; i++)
        {
            RewardButtonView button = _choiceBinder.GetButton(i);

            if (button == null)
                continue;

            if (i >= choiceCount)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            RewardChoiceData choice = choices[i];
            button.gameObject.SetActive(true);

            float delay = i * _settings.RefreshCardStagger;
            lastDelay = delay;
            Tween tween = button.CreateRefreshTween(
                choice,
                _choiceBinder.GetPresentation(choice),
                _choiceBinder.OnClicked,
                delay,
                _settings.RefreshOutDuration,
                _settings.RefreshInDuration,
                _settings.RefreshOutEase,
                _settings.RefreshInEase);

            _refreshSequence.Join(tween);
        }

        _refreshSequence.InsertCallback(
            lastDelay + _settings.RefreshOutDuration + 0.02f,
            _audioPlayer.PlayCardReveal);
        _refreshSequence.AppendCallback(() => applyRefreshedState?.Invoke(state));
        _refreshSequence.OnComplete(() => CompleteTransition(onComplete));
    }

    public void PlaySelectionDismiss(
        RewardChoiceData selectedChoice,
        Action requestClose)
    {
        EnsureAnimationStateCached();
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
        float selectedExitStart = InsertRewardDismissTweens(
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
        InsertTopExitTweens(_dismissSequence, topExitStart);

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
        EnsureAnimationStateCached();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        for (int i = 0; i < _topGroupStates.Length; i++)
            _topGroupStates[i].Reset();

        _actionControls.ResetLayout();
        _choiceBinder.ResetAnimatedState();
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

    private void EnsureAnimationStateCached()
    {
        if (_hasCachedAnimationState)
            return;

        _topGroupStates = CreateStates(_topSlideGroups);
        _hasCachedAnimationState = true;
    }

    private void PrepareTopGroupsForEnter()
    {
        for (int i = 0; i < _topGroupStates.Length; i++)
        {
            _topGroupStates[i].Kill();
            _topGroupStates[i].Prepare(_settings.TopEnterOffset, 0.98f);
        }
    }

    private void PrepareRewardButtonsForEnter()
    {
        for (int i = 0; i < _choiceBinder.ButtonCount; i++)
        {
            RewardButtonView button = _choiceBinder.GetButton(i);

            if (button != null && button.gameObject.activeSelf)
                button.PrepareEnter(_settings.RewardEnterOffset, 0.96f);
        }
    }

    private void InsertTopEnterTweens(Sequence sequence, float startTime)
    {
        for (int i = 0; i < _topGroupStates.Length; i++)
        {
            sequence.Insert(
                startTime,
                _topGroupStates[i].CreateEnterTween(
                    _settings.TopEnterDuration,
                    _settings.TopEnterEase,
                    Ease.OutBack));
        }
    }

    private void InsertRewardEnterTweens(Sequence sequence, float startTime)
    {
        for (int i = 0; i < _choiceBinder.ButtonCount; i++)
        {
            RewardButtonView button = _choiceBinder.GetButton(i);

            if (button == null || !button.gameObject.activeSelf)
                continue;

            sequence.Insert(
                startTime + i * _settings.RewardEnterStagger,
                button.CreateEnterTween(
                    _settings.RewardEnterDuration,
                    _settings.RewardEnterEase,
                    _settings.RewardScaleEase));
        }
    }

    private float InsertRewardDismissTweens(
        Sequence sequence,
        RewardButtonView selectedButton,
        float startTime)
    {
        int unselectedIndex = 0;
        float lastUnselectedExitEnd = startTime;

        for (int i = _choiceBinder.ButtonCount - 1; i >= 0; i--)
        {
            RewardButtonView button = _choiceBinder.GetButton(i);

            if (button == null || !button.gameObject.activeSelf)
                continue;

            if (button == selectedButton)
                continue;

            float delay = startTime + unselectedIndex * _settings.UnselectedExitStagger;
            sequence.Insert(
                delay,
                button.CreateUnselectedDismissTween(
                    _settings.UnselectedExitDuration,
                    _settings.UnselectedExitOffset,
                    _settings.UnselectedExitScaleMultiplier,
                    _settings.UnselectedExitEase));
            lastUnselectedExitEnd = Mathf.Max(lastUnselectedExitEnd, delay + _settings.UnselectedExitDuration);
            unselectedIndex++;
        }

        float selectedExitStart = Mathf.Max(
            _settings.SelectionFocusDuration,
            lastUnselectedExitEnd + _settings.UnselectedExitStagger);

        sequence.Insert(
            0f,
            selectedButton.CreateSelectedDismissTween(
                selectedExitStart,
                _settings.SelectionGrowDuration,
                _settings.SelectionExitDuration,
                _settings.SelectionExitOffset,
                _settings.SelectionScaleMultiplier,
                _settings.SelectionExitScaleMultiplier,
                _settings.SelectionFocusEase,
                _settings.SelectionExitEase));

        return selectedExitStart;
    }

    private void InsertTopExitTweens(Sequence sequence, float startTime)
    {
        for (int i = 0; i < _topGroupStates.Length; i++)
        {
            sequence.Insert(
                startTime,
                _topGroupStates[i].CreateExitTween(
                    _settings.TopExitOffset,
                    0.98f,
                    _settings.TopEnterDuration,
                    _settings.UnselectedExitEase,
                    _settings.UnselectedExitEase));
        }
    }

    private void KillAnimations()
    {
        if (_canvasGroup != null)
            _canvasGroup.DOKill();

        KillActiveSequences();

        if (_topGroupStates != null)
        {
            for (int i = 0; i < _topGroupStates.Length; i++)
                _topGroupStates[i].Kill();
        }

        _actionControls.KillAnimations();
        _choiceBinder.KillAnimations();
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

    private static RewardPopupRectTransformState[] CreateStates(RectTransform[] rects)
    {
        if (rects == null || rects.Length == 0)
            return Array.Empty<RewardPopupRectTransformState>();

        int count = 0;

        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] != null)
                count++;
        }

        RewardPopupRectTransformState[] states = new RewardPopupRectTransformState[count];
        int index = 0;

        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] != null)
                states[index++] = new RewardPopupRectTransformState(rects[i]);
        }

        return states;
    }

}
