using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormFaceVisualController : MonoBehaviour
{
    private const string IdleVisualName = "Visual_Idle";
    private const string AngryVisualName = "Visual_Engry";
    private const string AngryVisualFallbackName = "Visual_Angry";

    [Header("Visual Roots")]
    [SerializeField] private GameObject _idleVisual;

    [SerializeField] private GameObject _angryVisual;

    [Header("Idle Breathing")]
    [SerializeField] private bool _animateIdle = true;

    [SerializeField, Min(0.01f)] private float _idleBreathDuration = 0.85f;
    [SerializeField, Min(0f)] private float _idleBreathYOffset = 0.018f;
    [SerializeField, Range(0f, 0.2f)] private float _idleBreathScale = 0.032f;

    [Header("Boost Breathing")]
    [SerializeField] private bool _animateBoost = true;

    [SerializeField, Min(0.01f)] private float _boostPulseDuration = 0.18f;
    [SerializeField, Min(0f)] private float _boostPulseYOffset = 0.018f;
    [SerializeField, Range(0f, 0.25f)] private float _boostPulseScale = 0.055f;

    [Header("Switch Punch")]
    [SerializeField, Min(0.01f)] private float _switchPunchDuration = 0.08f;

    [SerializeField, Range(1f, 1.3f)] private float _switchPunchXScale = 1.08f;
    [SerializeField, Range(0.7f, 1f)] private float _switchPunchYScale = 0.92f;

    private Transform _idleTransform;
    private Transform _angryTransform;
    private Vector3 _idleBasePosition;
    private Vector3 _angryBasePosition;
    private Vector3 _idleBaseScale;
    private Vector3 _angryBaseScale;
    private Sequence _switchSequence;
    private Sequence _loopSequence;
    private bool _boostActive;
    private bool _initialized;

    private void Reset()
    {
        ResolveVisuals();
    }

    private void Awake()
    {
        ResolveVisuals();
        CacheBaseTransforms();
        ApplyState(false, true);
    }

    private void OnEnable()
    {
        ResolveVisuals();
        CacheBaseTransforms();
        ApplyState(_boostActive, true);
    }

    private void OnDisable()
    {
        KillSequences();
        RestoreBaseTransforms();
    }

    private void OnDestroy()
    {
        KillSequences();
    }

    public void SetBoostActive(bool active)
    {
        if (_initialized && _boostActive == active)
        {
            if (isActiveAndEnabled && (_loopSequence == null || !_loopSequence.IsActive()))
                ApplyState(active, true);

            return;
        }

        _boostActive = active;
        ApplyState(active, !isActiveAndEnabled);
    }

    private void ApplyState(bool boostActive, bool immediate)
    {
        ResolveVisuals();
        KillSequences();

        if (_initialized)
            RestoreBaseTransforms();

        CacheBaseTransforms();

        SetActive(_idleVisual, !boostActive);
        SetActive(_angryVisual, boostActive);

        Transform activeTransform = GetActiveTransform(boostActive);
        if (activeTransform == null)
            return;

        if (immediate)
        {
            StartLoop(boostActive);
            return;
        }

        Vector3 baseScale = GetBaseScale(boostActive);
        Vector3 punchScale = new(
            baseScale.x * _switchPunchXScale,
            baseScale.y * _switchPunchYScale,
            baseScale.z);

        _switchSequence = DOTween.Sequence()
            .SetTarget(this)
            .Append(activeTransform.DOScale(punchScale, _switchPunchDuration).SetEase(Ease.OutSine))
            .Append(activeTransform.DOScale(baseScale, _switchPunchDuration * 1.45f).SetEase(Ease.OutBack))
            .OnComplete(() =>
            {
                _switchSequence = null;
                StartLoop(boostActive);
            });
    }

    private void StartLoop(bool boostActive)
    {
        _loopSequence?.Kill();
        _loopSequence = null;

        Transform target = GetActiveTransform(boostActive);
        if (target == null || !target.gameObject.activeInHierarchy)
            return;

        if (boostActive)
            StartBoostLoop(target);
        else
            StartIdleLoop(target);
    }

    private void StartIdleLoop(Transform target)
    {
        if (!_animateIdle)
            return;

        Vector3 basePosition = _idleBasePosition;
        Vector3 baseScale = _idleBaseScale;
        Vector3 breathPosition = basePosition + new Vector3(0f, _idleBreathYOffset, 0f);
        Vector3 breathScale = new(
            baseScale.x * (1f + _idleBreathScale),
            baseScale.y * (1f - (_idleBreathScale * 0.55f)),
            baseScale.z);

        _loopSequence = DOTween.Sequence()
            .SetTarget(this)
            .Append(target.DOLocalMove(breathPosition, _idleBreathDuration).SetEase(Ease.InOutSine))
            .Join(target.DOScale(breathScale, _idleBreathDuration).SetEase(Ease.InOutSine))
            .Append(target.DOLocalMove(basePosition, _idleBreathDuration).SetEase(Ease.InOutSine))
            .Join(target.DOScale(baseScale, _idleBreathDuration).SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Restart);
    }

    private void StartBoostLoop(Transform target)
    {
        if (!_animateBoost)
            return;

        Vector3 basePosition = _angryBasePosition;
        Vector3 baseScale = _angryBaseScale;
        Vector3 pulsePosition = basePosition + new Vector3(0f, _boostPulseYOffset, 0f);
        Vector3 pulseScale = new(
            baseScale.x * (1f + _boostPulseScale),
            baseScale.y * (1f - (_boostPulseScale * 0.75f)),
            baseScale.z);

        _loopSequence = DOTween.Sequence()
            .SetTarget(this)
            .Append(target.DOLocalMove(pulsePosition, _boostPulseDuration).SetEase(Ease.InOutSine))
            .Join(target.DOScale(pulseScale, _boostPulseDuration).SetEase(Ease.InOutSine))
            .Append(target.DOLocalMove(basePosition, _boostPulseDuration).SetEase(Ease.InOutSine))
            .Join(target.DOScale(baseScale, _boostPulseDuration).SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Restart);
    }

    private void ResolveVisuals()
    {
        if (_idleVisual == null)
            _idleVisual = FindChildGameObject(transform, IdleVisualName);

        if (_angryVisual == null)
            _angryVisual = FindChildGameObject(transform, AngryVisualName);

        if (_angryVisual == null)
            _angryVisual = FindChildGameObject(transform, AngryVisualFallbackName);

        _idleTransform = _idleVisual != null ? _idleVisual.transform : null;
        _angryTransform = _angryVisual != null ? _angryVisual.transform : null;
    }

    private void CacheBaseTransforms()
    {
        if (_idleTransform != null)
        {
            _idleBasePosition = _idleTransform.localPosition;
            _idleBaseScale = _idleTransform.localScale;
        }

        if (_angryTransform != null)
        {
            _angryBasePosition = _angryTransform.localPosition;
            _angryBaseScale = _angryTransform.localScale;
        }

        _initialized = true;
    }

    private void RestoreBaseTransforms()
    {
        if (_idleTransform != null)
        {
            _idleTransform.localPosition = _idleBasePosition;
            _idleTransform.localScale = _idleBaseScale;
        }

        if (_angryTransform != null)
        {
            _angryTransform.localPosition = _angryBasePosition;
            _angryTransform.localScale = _angryBaseScale;
        }

    }

    private Transform GetActiveTransform(bool boostActive)
    {
        return boostActive ? _angryTransform : _idleTransform;
    }

    private Vector3 GetBaseScale(bool boostActive)
    {
        return boostActive ? _angryBaseScale : _idleBaseScale;
    }

    private void KillSequences()
    {
        _switchSequence?.Kill();
        _switchSequence = null;

        _loopSequence?.Kill();
        _loopSequence = null;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }

    private static GameObject FindChildGameObject(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child.gameObject;

            GameObject nested = FindChildGameObject(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
