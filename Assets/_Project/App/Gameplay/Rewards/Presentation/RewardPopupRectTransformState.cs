using DG.Tweening;
using UnityEngine;

public readonly struct RewardPopupRectTransformState
{
    private readonly RectTransform _rectTransform;
    private readonly Vector2 _anchoredPosition;
    private readonly Vector3 _localScale;

    public RewardPopupRectTransformState(RectTransform rectTransform)
    {
        _rectTransform = rectTransform;
        _anchoredPosition = rectTransform != null
            ? rectTransform.anchoredPosition
            : Vector2.zero;
        _localScale = rectTransform != null
            ? rectTransform.localScale
            : Vector3.one;
    }

    public RectTransform RectTransform => _rectTransform;
    public Vector2 BaseAnchoredPosition => _anchoredPosition;
    public bool IsActive => _rectTransform != null && _rectTransform.gameObject.activeSelf;

    public void Prepare(float yOffset, float scaleMultiplier)
    {
        Prepare(_anchoredPosition, yOffset, scaleMultiplier);
    }

    public void Prepare(
        Vector2 targetAnchoredPosition,
        float yOffset,
        float scaleMultiplier)
    {
        if (_rectTransform == null)
            return;

        _rectTransform.anchoredPosition = targetAnchoredPosition + new Vector2(0f, yOffset);
        _rectTransform.localScale = _localScale * scaleMultiplier;
    }

    public Tween CreateEnterTween(float duration, Ease moveEase, Ease scaleEase)
    {
        return CreateEnterTween(_anchoredPosition, duration, moveEase, scaleEase);
    }

    public Tween CreateEnterTween(
        Vector2 targetAnchoredPosition,
        float duration,
        Ease moveEase,
        Ease scaleEase)
    {
        Sequence sequence = DOTween.Sequence();

        if (_rectTransform == null)
            return sequence;

        sequence.Join(_rectTransform.DOAnchorPos(targetAnchoredPosition, duration).SetEase(moveEase));
        sequence.Join(_rectTransform.DOScale(_localScale, duration).SetEase(scaleEase));
        return sequence;
    }

    public Tween CreateExitTween(
        float yOffset,
        float scaleMultiplier,
        float duration,
        Ease moveEase,
        Ease scaleEase)
    {
        return CreateExitTween(
            _anchoredPosition,
            yOffset,
            scaleMultiplier,
            duration,
            moveEase,
            scaleEase);
    }

    public Tween CreateExitTween(
        Vector2 targetAnchoredPosition,
        float yOffset,
        float scaleMultiplier,
        float duration,
        Ease moveEase,
        Ease scaleEase)
    {
        Sequence sequence = DOTween.Sequence();

        if (_rectTransform == null)
            return sequence;

        sequence.Join(_rectTransform.DOAnchorPos(targetAnchoredPosition + new Vector2(0f, yOffset), duration).SetEase(moveEase));
        sequence.Join(_rectTransform.DOScale(_localScale * scaleMultiplier, duration).SetEase(scaleEase));
        return sequence;
    }

    public void Reset()
    {
        Reset(_anchoredPosition);
    }

    public void Reset(Vector2 targetAnchoredPosition)
    {
        if (_rectTransform == null)
            return;

        Kill();
        _rectTransform.anchoredPosition = targetAnchoredPosition;
        _rectTransform.localScale = _localScale;
    }

    public void Kill()
    {
        if (_rectTransform != null)
            _rectTransform.DOKill();
    }
}
