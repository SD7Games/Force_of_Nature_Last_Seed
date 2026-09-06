using System;
using DG.Tweening;
using UnityEngine;

internal sealed class RewardPopupAnimatedLayout
{
    private const float TopEnterStartSeconds = 0.02f;
    private const float RewardEnterStartSeconds = 0.1f;
    private const float ActionEnterStartSeconds = 0.3f;
    private const float TopPreparedScale = 0.98f;
    private const float RewardPreparedScale = 0.96f;

    private readonly RectTransform[] _topSlideGroups;
    private readonly RewardPopupChoiceBinder _choiceBinder;
    private readonly RewardPopupActionControls _actionControls;
    private readonly RewardPopupAnimationSettings _settings;

    private RewardPopupRectTransformState[] _topGroupStates;

    public RewardPopupAnimatedLayout(
        RectTransform[] topSlideGroups,
        RewardPopupChoiceBinder choiceBinder,
        RewardPopupActionControls actionControls,
        RewardPopupAnimationSettings settings)
    {
        _topSlideGroups = topSlideGroups;
        _choiceBinder = choiceBinder;
        _actionControls = actionControls;
        _settings = settings;
    }

    public void PrepareForShow()
    {
        EnsureStateCached();

        for (int i = 0; i < _topGroupStates.Length; i++)
        {
            _topGroupStates[i].Kill();
            _topGroupStates[i].Prepare(_settings.TopEnterOffset, TopPreparedScale);
        }

        for (int i = 0; i < _choiceBinder.ButtonCount; i++)
        {
            RewardButtonView button = _choiceBinder.GetButton(i);

            if (button != null && button.gameObject.activeSelf)
                button.PrepareEnter(_settings.RewardEnterOffset, RewardPreparedScale);
        }

        _actionControls.PrepareForEnter(_settings.ActionEnterOffset);
    }

    public void InsertShowTweens(Sequence sequence)
    {
        EnsureStateCached();

        for (int i = 0; i < _topGroupStates.Length; i++)
        {
            sequence.Insert(
                TopEnterStartSeconds,
                _topGroupStates[i].CreateEnterTween(
                    _settings.TopEnterDuration,
                    _settings.TopEnterEase,
                    Ease.OutBack));
        }

        for (int i = 0; i < _choiceBinder.ButtonCount; i++)
        {
            RewardButtonView button = _choiceBinder.GetButton(i);

            if (button == null || !button.gameObject.activeSelf)
                continue;

            sequence.Insert(
                RewardEnterStartSeconds + i * _settings.RewardEnterStagger,
                button.CreateEnterTween(
                    _settings.RewardEnterDuration,
                    _settings.RewardEnterEase,
                    _settings.RewardScaleEase));
        }

        _actionControls.InsertEnterTweens(
            sequence,
            ActionEnterStartSeconds,
            _settings.ActionEnterDuration);
    }

    public float InsertRewardDismissTweens(
        Sequence sequence,
        RewardButtonView selectedButton,
        float startTime)
    {
        int unselectedIndex = 0;
        float lastUnselectedExitEnd = startTime;

        for (int i = _choiceBinder.ButtonCount - 1; i >= 0; i--)
        {
            RewardButtonView button = _choiceBinder.GetButton(i);

            if (button == null || !button.gameObject.activeSelf || button == selectedButton)
                continue;

            float delay = startTime + unselectedIndex * _settings.UnselectedExitStagger;
            sequence.Insert(
                delay,
                button.CreateUnselectedDismissTween(
                    _settings.UnselectedExitDuration,
                    _settings.UnselectedExitOffset,
                    _settings.UnselectedExitScaleMultiplier,
                    _settings.UnselectedExitEase));
            lastUnselectedExitEnd = Mathf.Max(
                lastUnselectedExitEnd,
                delay + _settings.UnselectedExitDuration);
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

    public void InsertTopExitTweens(Sequence sequence, float startTime)
    {
        EnsureStateCached();

        for (int i = 0; i < _topGroupStates.Length; i++)
        {
            sequence.Insert(
                startTime,
                _topGroupStates[i].CreateExitTween(
                    _settings.TopExitOffset,
                    TopPreparedScale,
                    _settings.TopEnterDuration,
                    _settings.UnselectedExitEase,
                    _settings.UnselectedExitEase));
        }
    }

    public void Reset()
    {
        EnsureStateCached();

        for (int i = 0; i < _topGroupStates.Length; i++)
            _topGroupStates[i].Reset();

        _actionControls.ResetLayout();
        _choiceBinder.ResetAnimatedState();
    }

    public void KillAnimations()
    {
        if (_topGroupStates != null)
        {
            for (int i = 0; i < _topGroupStates.Length; i++)
                _topGroupStates[i].Kill();
        }

        _actionControls.KillAnimations();
        _choiceBinder.KillAnimations();
    }

    private void EnsureStateCached()
    {
        if (_topGroupStates != null)
            return;

        _topGroupStates = CreateStates(_topSlideGroups);
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
