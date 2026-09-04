using System;
using DG.Tweening;
using UnityEngine;

public sealed class RewardButtonAnimator
{
    private readonly RectTransform _root;
    private readonly CanvasGroup _canvasGroup;
    private readonly RectTransform _icon;
    private Vector2 _basePosition;
    private Vector3 _baseScale;
    private Vector3 _baseIconScale;

    public RewardButtonAnimator(
        RectTransform root,
        CanvasGroup canvasGroup,
        RectTransform icon)
    {
        _root = root;
        _canvasGroup = canvasGroup;
        _icon = icon;
        _basePosition = root != null ? root.anchoredPosition : default;
        _baseScale = root != null ? root.localScale : Vector3.one;
        CaptureIconScale();
    }

    public RectTransform Root => _root;

    public void CaptureIconScale()
    {
        _baseIconScale = _icon != null ? _icon.localScale : Vector3.one;
    }

    public void Kill()
    {
        _root?.DOKill();
        _canvasGroup?.DOKill();
        _icon?.DOKill();
    }

    public void Reset()
    {
        Kill();

        if (_root != null)
        {
            _root.anchoredPosition = _basePosition;
            _root.localScale = _baseScale;
        }

        if (_icon != null)
            _icon.localScale = _baseIconScale;

        SetAlpha(1f);
    }

    public void PrepareEnter(float yOffset, float scaleMultiplier)
    {
        Kill();

        if (_root != null)
        {
            _root.anchoredPosition = _basePosition + new Vector2(0f, yOffset);
            _root.localScale = _baseScale * scaleMultiplier;
        }

        if (_icon != null)
            _icon.localScale = _baseIconScale * 0.86f;

        SetAlpha(0f);
    }

    public Tween CreateEnter(float duration, Ease moveEase, Ease scaleEase)
    {
        Sequence sequence = DOTween.Sequence();

        if (_root != null)
        {
            sequence.Join(_root.DOAnchorPos(_basePosition, duration).SetEase(moveEase));
            sequence.Join(_root.DOScale(_baseScale, duration).SetEase(scaleEase));
        }

        if (_canvasGroup != null)
            sequence.Join(_canvasGroup.DOFade(1f, duration * 0.72f).SetEase(Ease.OutSine));

        if (_icon != null)
            sequence.Join(_icon.DOScale(_baseIconScale, duration * 0.78f).SetEase(Ease.OutBack));

        return sequence;
    }

    public Tween CreateRefresh(
        Action replaceContent,
        float delay,
        float outDuration,
        float inDuration,
        Ease outEase,
        Ease inEase)
    {
        Kill();
        Sequence sequence = DOTween.Sequence();

        if (delay > 0f)
            sequence.AppendInterval(delay);

        if (_root != null)
        {
            sequence.Append(_root.DOShakeAnchorPos(
                outDuration,
                new Vector2(0f, 10f),
                8,
                45f,
                false,
                true));
            sequence.Join(_root.DOScale(_baseScale * 0.96f, outDuration).SetEase(outEase));
        }
        else
        {
            sequence.AppendInterval(outDuration);
        }

        if (_canvasGroup != null)
            sequence.Join(_canvasGroup.DOFade(0f, outDuration).SetEase(Ease.InSine));

        sequence.AppendCallback(() =>
        {
            replaceContent?.Invoke();
            SetAlpha(0f);

            if (_root != null)
            {
                _root.anchoredPosition = _basePosition;
                _root.localScale = _baseScale * 0.96f;
            }

            if (_icon != null)
                _icon.localScale = _baseIconScale * 0.62f;
        });

        if (_root != null)
            sequence.Append(_root.DOScale(_baseScale, inDuration).SetEase(inEase));
        else
            sequence.AppendInterval(inDuration);

        if (_canvasGroup != null)
            sequence.Join(_canvasGroup.DOFade(1f, inDuration * 0.84f).SetEase(Ease.OutSine));

        if (_icon != null)
            sequence.Join(_icon.DOScale(_baseIconScale, inDuration).SetEase(Ease.OutBack));

        return sequence;
    }

    public Tween CreateSelectedDismiss(
        float focusDuration,
        float growDuration,
        float exitDuration,
        float exitYOffset,
        float focusScaleMultiplier,
        float exitScaleMultiplier,
        Ease focusEase,
        Ease exitEase)
    {
        Kill();
        Sequence sequence = DOTween.Sequence();
        float safeGrowDuration = Mathf.Max(0f, growDuration);
        float safeFocusDuration = Mathf.Max(0f, focusDuration);

        if (_root != null && safeGrowDuration > 0f)
        {
            sequence.Append(_root.DOScale(
                _baseScale * focusScaleMultiplier,
                safeGrowDuration).SetEase(focusEase));

            if (_icon != null)
            {
                sequence.Join(_icon.DOScale(
                    _baseIconScale * focusScaleMultiplier,
                    safeGrowDuration).SetEase(focusEase));
            }
        }
        else
        {
            sequence.AppendInterval(safeGrowDuration);
        }

        float holdDuration = Mathf.Max(0f, safeFocusDuration - safeGrowDuration);

        if (holdDuration > 0f)
            sequence.AppendInterval(holdDuration);

        AppendExit(sequence, exitDuration, exitYOffset, exitScaleMultiplier, exitEase, true);
        return sequence;
    }

    public Tween CreateUnselectedDismiss(
        float duration,
        float exitYOffset,
        float exitScaleMultiplier,
        Ease exitEase)
    {
        Kill();
        Sequence sequence = DOTween.Sequence();
        AppendExit(sequence, duration, exitYOffset, exitScaleMultiplier, exitEase, false);
        return sequence;
    }

    private void AppendExit(
        Sequence sequence,
        float duration,
        float yOffset,
        float scaleMultiplier,
        Ease ease,
        bool appendMovement)
    {
        if (_root != null)
        {
            Tween movement = _root.DOAnchorPos(
                _basePosition + new Vector2(0f, yOffset),
                duration).SetEase(ease);

            if (appendMovement)
                sequence.Append(movement);
            else
                sequence.Join(movement);

            sequence.Join(_root.DOScale(_baseScale * scaleMultiplier, duration).SetEase(ease));
        }
        else
        {
            sequence.AppendInterval(duration);
        }

        if (_canvasGroup != null)
            sequence.Join(_canvasGroup.DOFade(0f, duration).SetEase(Ease.InSine));

        if (_icon != null)
            sequence.Join(_icon.DOScale(_baseIconScale * scaleMultiplier, duration).SetEase(ease));
    }

    private void SetAlpha(float alpha)
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = alpha;
    }
}
