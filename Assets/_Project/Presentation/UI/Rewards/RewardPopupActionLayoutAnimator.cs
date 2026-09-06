using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class RewardPopupActionLayoutAnimator
{
    private const float ActionStaggerSeconds = 0.035f;
    private const float PreparedScale = 0.98f;

    private readonly Button _rerollButton;
    private readonly Button _adRerollButton;
    private readonly Button _takeAllButton;
    private readonly TMP_Text _rerollAttemptsText;
    private readonly TMP_Text _adRerollAttemptsText;
    private readonly TMP_Text _takeAllAttemptsText;
    private readonly float _singleActionButtonAnchoredX;

    private RewardPopupRectTransformState[] _buttonStates;
    private RewardPopupRectTransformState _rerollAttemptsTextState;
    private RewardPopupRectTransformState _adRerollAttemptsTextState;
    private RewardPopupRectTransformState _takeAllAttemptsTextState;
    private RewardPopupState _currentState;
    private bool _hasCurrentState;
    private bool _hasCachedLayoutState;

    public RewardPopupActionLayoutAnimator(
        Button rerollButton,
        Button adRerollButton,
        Button takeAllButton,
        TMP_Text rerollAttemptsText,
        TMP_Text adRerollAttemptsText,
        TMP_Text takeAllAttemptsText,
        float singleActionButtonAnchoredX)
    {
        _rerollButton = rerollButton;
        _adRerollButton = adRerollButton;
        _takeAllButton = takeAllButton;
        _rerollAttemptsText = rerollAttemptsText;
        _adRerollAttemptsText = adRerollAttemptsText;
        _takeAllAttemptsText = takeAllAttemptsText;
        _singleActionButtonAnchoredX = singleActionButtonAnchoredX;
    }

    public void SetState(RewardPopupState state)
    {
        _currentState = state;
        _hasCurrentState = true;
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
                PreparedScale);
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
                startTime + activeIndex * ActionStaggerSeconds,
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
                startTime + activeIndex * ActionStaggerSeconds,
                _buttonStates[i].CreateExitTween(
                    GetActionButtonTargetPosition(_buttonStates[i]),
                    yOffset,
                    PreparedScale,
                    duration,
                    exitEase,
                    exitEase));
            activeIndex++;
        }
    }

    public void Reset()
    {
        EnsureLayoutCached();
        ResetCachedLayout();
    }

    public void ResetIfCached()
    {
        if (_hasCachedLayoutState)
            ResetCachedLayout();
    }

    public void KillAnimations()
    {
        if (_buttonStates == null)
            return;

        for (int i = 0; i < _buttonStates.Length; i++)
            _buttonStates[i].Kill();
    }

    private void ResetCachedLayout()
    {
        for (int i = 0; i < _buttonStates.Length; i++)
            _buttonStates[i].Reset(GetActionButtonTargetPosition(_buttonStates[i]));

        _rerollAttemptsTextState.Reset();
        _adRerollAttemptsTextState.Reset();
        _takeAllAttemptsTextState.Reset();
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
        if (!_hasCurrentState || _currentState.UseTakeAllButton)
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

    private static RectTransform GetRectTransform(Button button)
    {
        return button != null ? button.transform as RectTransform : null;
    }

    private static RectTransform GetRectTransform(TMP_Text text)
    {
        return text != null ? text.transform as RectTransform : null;
    }
}
