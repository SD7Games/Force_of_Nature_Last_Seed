using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class UiIdleTweenAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _target;
    [SerializeField] private Image[] _blinkImages;

    [Header("Idle Motion")]
    [SerializeField] private bool _animatePosition = true;
    [SerializeField] private Vector2 _positionOffset = new Vector2(0f, 10f);
    [SerializeField] private bool _animateScale = true;
    [SerializeField, Min(0.01f)] private float _scaleMultiplier = 1.03f;
    [SerializeField, Min(0.05f)] private float _halfCycleDuration = 1.25f;
    [SerializeField] private Ease _idleEase = Ease.InOutSine;

    [Header("Blink")]
    [SerializeField] private bool _animateBlink;
    [SerializeField, Range(0f, 1f)] private float _openFillAmount = 1f;
    [SerializeField, Range(0f, 1f)] private float _closedFillAmount;
    [SerializeField, Min(0.1f)] private float _blinkInterval = 3f;
    [SerializeField, Min(0f)] private float _blinkCloseDuration = 0.06f;
    [SerializeField, Min(0f)] private float _blinkClosedPause = 0.04f;
    [SerializeField, Min(0f)] private float _blinkOpenDuration = 0.12f;
    [SerializeField] private Ease _blinkEase = Ease.OutQuad;

    [Header("Playback")]
    [SerializeField] private bool _playOnEnable = true;
    [SerializeField] private bool _useUnscaledTime;
    [SerializeField] private bool _restoreOnDisable = true;

    private Sequence _idleSequence;
    private Sequence _blinkSequence;
    private Vector2 _baseAnchoredPosition;
    private Vector3 _baseLocalScale;
    private float[] _baseBlinkFillAmounts = new float[0];
    private bool _isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();

        if (_playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop(_restoreOnDisable);
    }

    private void OnDestroy()
    {
        Stop(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _scaleMultiplier = Mathf.Max(0.01f, _scaleMultiplier);
        _halfCycleDuration = Mathf.Max(0.05f, _halfCycleDuration);
        _openFillAmount = Mathf.Clamp01(_openFillAmount);
        _closedFillAmount = Mathf.Clamp01(_closedFillAmount);
        _blinkInterval = Mathf.Max(0.1f, _blinkInterval);
        _blinkCloseDuration = Mathf.Max(0f, _blinkCloseDuration);
        _blinkClosedPause = Mathf.Max(0f, _blinkClosedPause);
        _blinkOpenDuration = Mathf.Max(0f, _blinkOpenDuration);

        if (_target == null)
            TryGetComponent(out _target);
    }
#endif

    public void Play()
    {
        Initialize();
        Stop(false);
        RestoreState();
        StartIdleLoop();
        StartBlinkLoop();
    }

    public void Stop(bool restoreState)
    {
        KillSequence(ref _idleSequence);
        KillSequence(ref _blinkSequence);

        if (restoreState && _isInitialized)
            RestoreState();
    }

    private void Initialize()
    {
        if (_isInitialized)
            return;

        if (_target == null)
            TryGetComponent(out _target);

        if (_target != null)
        {
            _baseAnchoredPosition = _target.anchoredPosition;
            _baseLocalScale = _target.localScale;
        }

        CacheBlinkFillAmounts();
        _isInitialized = true;
    }

    private void CacheBlinkFillAmounts()
    {
        if (_blinkImages == null || _blinkImages.Length == 0)
        {
            _baseBlinkFillAmounts = new float[0];
            return;
        }

        _baseBlinkFillAmounts = new float[_blinkImages.Length];

        for (int i = 0; i < _blinkImages.Length; i++)
        {
            Image image = _blinkImages[i];
            _baseBlinkFillAmounts[i] = image != null ? image.fillAmount : _openFillAmount;
        }
    }

    private void StartIdleLoop()
    {
        if (_target == null || (!HasPositionAnimation() && !HasScaleAnimation()))
            return;

        _idleSequence = DOTween.Sequence()
            .SetUpdate(_useUnscaledTime)
            .SetTarget(this);

        AppendIdleStep(_baseAnchoredPosition + _positionOffset, GetScaledTarget(), _halfCycleDuration);
        AppendIdleStep(_baseAnchoredPosition, _baseLocalScale, _halfCycleDuration);

        _idleSequence.SetLoops(-1, LoopType.Restart);
    }

    private void AppendIdleStep(Vector2 targetPosition, Vector3 targetScale, float duration)
    {
        bool appendedTween = false;

        if (HasPositionAnimation())
        {
            _idleSequence.Append(_target
                .DOAnchorPos(targetPosition, duration)
                .SetEase(_idleEase));

            appendedTween = true;
        }

        if (HasScaleAnimation())
        {
            Tween scaleTween = _target
                .DOScale(targetScale, duration)
                .SetEase(_idleEase);

            if (appendedTween)
                _idleSequence.Join(scaleTween);
            else
                _idleSequence.Append(scaleTween);
        }
    }

    private void StartBlinkLoop()
    {
        if (!_animateBlink || _blinkImages == null || _blinkImages.Length == 0)
            return;

        SetBlinkFill(_openFillAmount);

        _blinkSequence = DOTween.Sequence()
            .SetUpdate(_useUnscaledTime)
            .SetTarget(this);

        _blinkSequence.AppendInterval(_blinkInterval);
        AppendBlinkFill(_closedFillAmount, _blinkCloseDuration);

        if (_blinkClosedPause > 0f)
            _blinkSequence.AppendInterval(_blinkClosedPause);

        AppendBlinkFill(_openFillAmount, _blinkOpenDuration);
        _blinkSequence.SetLoops(-1, LoopType.Restart);
    }

    private void AppendBlinkFill(float fillAmount, float duration)
    {
        if (duration <= 0f)
        {
            _blinkSequence.AppendCallback(() => SetBlinkFill(fillAmount));
            return;
        }

        bool appendedTween = false;

        for (int i = 0; i < _blinkImages.Length; i++)
        {
            Image image = _blinkImages[i];

            if (image == null)
                continue;

            Tween tween = image
                .DOFillAmount(fillAmount, duration)
                .SetEase(_blinkEase);

            if (appendedTween)
                _blinkSequence.Join(tween);
            else
                _blinkSequence.Append(tween);

            appendedTween = true;
        }
    }

    private void RestoreState()
    {
        if (_target != null)
        {
            _target.anchoredPosition = _baseAnchoredPosition;
            _target.localScale = _baseLocalScale;
        }

        RestoreBlinkFillAmounts();
    }

    private void RestoreBlinkFillAmounts()
    {
        if (_blinkImages == null || _baseBlinkFillAmounts == null)
            return;

        int count = Mathf.Min(_blinkImages.Length, _baseBlinkFillAmounts.Length);

        for (int i = 0; i < count; i++)
        {
            Image image = _blinkImages[i];

            if (image != null)
                image.fillAmount = _baseBlinkFillAmounts[i];
        }
    }

    private void SetBlinkFill(float fillAmount)
    {
        if (_blinkImages == null)
            return;

        for (int i = 0; i < _blinkImages.Length; i++)
        {
            Image image = _blinkImages[i];

            if (image != null)
                image.fillAmount = fillAmount;
        }
    }

    private bool HasPositionAnimation()
    {
        return _animatePosition && _positionOffset.sqrMagnitude > 0f;
    }

    private bool HasScaleAnimation()
    {
        return _animateScale && !Mathf.Approximately(_scaleMultiplier, 1f);
    }

    private Vector3 GetScaledTarget()
    {
        return _baseLocalScale * _scaleMultiplier;
    }

    private static void KillSequence(ref Sequence sequence)
    {
        if (sequence == null)
            return;

        sequence.Kill();
        sequence = null;
    }
}
