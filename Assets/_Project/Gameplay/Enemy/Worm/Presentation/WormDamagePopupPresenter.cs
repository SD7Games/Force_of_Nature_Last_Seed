using System.Collections.Generic;
using LastSeed.Gameplay.Signals;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public sealed class WormDamagePopupPresenter : MonoBehaviour
{
    [SerializeField] private WormDamagePopupView _popupPrefab;
    [SerializeField, Min(0)] private int _initialPoolSize = 20;

    [Header("Mobile Budget")]
    [SerializeField, Min(0)] private int _normalPopupLimit = 4;
    [SerializeField, Min(0)] private int _damageOverTimePopupLimit = 1;
    [SerializeField, Min(0)] private int _visiblePopupSoftLimit = 8;
    [SerializeField, Min(0)] private int _fastAnimationThreshold = 6;
    [SerializeField, Min(0)] private int _accelerateOldNormalThreshold = 7;
    [SerializeField, Min(0f)] private float _sameAreaCullRadius = 0.65f;
    [SerializeField, Min(0)] private int _crowdedScaleThreshold = 8;
    [SerializeField, Min(0)] private int _veryCrowdedScaleThreshold = 14;
    [SerializeField, Range(0.1f, 1f)] private float _crowdedScaleMultiplier = 0.75f;
    [SerializeField, Range(0.1f, 1f)] private float _veryCrowdedScaleMultiplier = 0.55f;

    private ObjectPool<WormDamagePopupView> _pool;
    private readonly List<WormDamagePopupView> _activePopups = new();
    private readonly List<WormDamagePopupView> _activeNonCriticalPopups = new();

    private int _activeCount;
    private int _activeNormalCount;
    private int _activeDamageOverTimeCount;
    private System.Action<WormDamagePopupView> _popupCompleteHandler;
    private SignalBus _signalBus;
    private bool _isSubscribedToSignals;

    [Inject]
    public void Construct(SignalBus signalBus)
    {
        _signalBus = signalBus;
        SubscribeToSignals();
    }

    private void Awake()
    {
        _popupCompleteHandler = OnPopupComplete;

        if (_popupPrefab == null)
            Debug.LogError("DamagePopupPresenter: Popup prefab not assigned", this);

        if (_popupPrefab != null)
        {
            _pool = new ObjectPool<WormDamagePopupView>(CreatePopup, DeactivatePopup);
            _pool.Prewarm(_initialPoolSize);
        }
    }

    private void OnEnable()
    {
        SubscribeToSignals();
    }

    private void OnDisable()
    {
        UnsubscribeFromSignals();
    }

    private void OnDamageDealt(WormDamageDealtSignal signal)
    {
        DamageViewRequest request = signal.Request;
        bool isCritical = IsCritical(request);

        if (!CanShow(request, isCritical))
            return;

        WormDamagePopupView popup = _pool?.Rent();

        if (popup == null)
            return;

        WormDamagePopupView.AnimationMode animationMode = GetAnimationMode(request, isCritical);

        _activeCount++;
        _activePopups.Add(popup);
        float scaleMultiplier = GetScaleMultiplier();

        if (!isCritical)
            RegisterNonCriticalPopup(popup, request.Kind);

        popup.gameObject.SetActive(true);
        popup.Show(request, animationMode, scaleMultiplier, _popupCompleteHandler);
    }

    private void SubscribeToSignals()
    {
        if (_signalBus == null || _isSubscribedToSignals || !isActiveAndEnabled)
            return;

        _signalBus.Subscribe<WormDamageDealtSignal>(OnDamageDealt);
        _isSubscribedToSignals = true;
    }

    private void UnsubscribeFromSignals()
    {
        if (_signalBus == null || !_isSubscribedToSignals)
            return;

        _signalBus.Unsubscribe<WormDamageDealtSignal>(OnDamageDealt);
        _isSubscribedToSignals = false;
    }

    private void OnPopupComplete(WormDamagePopupView view)
    {
        _activeCount = Mathf.Max(0, _activeCount - 1);

        if (view != null && !view.IsCritical)
            UnregisterNonCriticalPopup(view);

        UnregisterActivePopup(view);
        _pool?.Return(view);
    }

    public void ClearActivePopups()
    {
        for (int i = _activePopups.Count - 1; i >= 0; i--)
        {
            _pool?.Return(_activePopups[i]);
        }

        _activePopups.Clear();
        _activeNonCriticalPopups.Clear();
        _activeCount = 0;
        _activeNormalCount = 0;
        _activeDamageOverTimeCount = 0;
    }

    private bool CanShow(DamageViewRequest request, bool isCritical)
    {
        if (isCritical)
            return true;

        RecountNonCriticalPopups();

        if (IsSameAreaCovered(request.WorldPosition))
            return false;

        if (IsNonCriticalKindAtLimit(request.Kind))
        {
            AccelerateOldestNonCriticalPopup();
            return false;
        }

        if (_activeCount >= _visiblePopupSoftLimit)
        {
            AccelerateOldestNonCriticalPopup();
            return false;
        }

        if (_accelerateOldNormalThreshold > 0 && _activeCount >= _accelerateOldNormalThreshold)
            AccelerateOldestNonCriticalPopup();

        return true;
    }

    private WormDamagePopupView CreatePopup()
    {
        if (_popupPrefab == null)
            return null;

        WormDamagePopupView popup = Instantiate(_popupPrefab, transform);
        popup.PrewarmText();
        return popup;
    }

    private static void DeactivatePopup(WormDamagePopupView popup)
    {
        popup.gameObject.SetActive(false);
    }

    private static bool IsCritical(DamageViewRequest request)
    {
        return request.IsCritical || request.Kind == DamageKind.Critical;
    }

    private WormDamagePopupView.AnimationMode GetAnimationMode(DamageViewRequest request, bool isCritical)
    {
        if (isCritical)
            return WormDamagePopupView.AnimationMode.Critical;

        if (request.Kind == DamageKind.DamageOverTime || _activeCount >= _fastAnimationThreshold)
            return WormDamagePopupView.AnimationMode.Fast;

        return WormDamagePopupView.AnimationMode.Normal;
    }

    private float GetScaleMultiplier()
    {
        if (_veryCrowdedScaleThreshold > 0 && _activeCount >= _veryCrowdedScaleThreshold)
            return _veryCrowdedScaleMultiplier;

        if (_crowdedScaleThreshold > 0 && _activeCount >= _crowdedScaleThreshold)
            return _crowdedScaleMultiplier;

        return 1f;
    }

    private void RegisterNonCriticalPopup(WormDamagePopupView popup, DamageKind kind)
    {
        _activeNonCriticalPopups.Add(popup);

        if (kind == DamageKind.DamageOverTime)
            _activeDamageOverTimeCount++;
        else
            _activeNormalCount++;
    }

    private void UnregisterNonCriticalPopup(WormDamagePopupView popup)
    {
        _activeNonCriticalPopups.Remove(popup);

        if (popup.Kind == DamageKind.DamageOverTime)
            _activeDamageOverTimeCount = Mathf.Max(0, _activeDamageOverTimeCount - 1);
        else
            _activeNormalCount = Mathf.Max(0, _activeNormalCount - 1);
    }

    private void UnregisterActivePopup(WormDamagePopupView popup)
    {
        if (popup != null)
            _activePopups.Remove(popup);
    }

    private bool IsNonCriticalKindAtLimit(DamageKind kind)
    {
        if (kind == DamageKind.DamageOverTime)
            return _activeDamageOverTimeCount >= _damageOverTimePopupLimit;

        return _activeNormalCount >= _normalPopupLimit;
    }

    private bool IsSameAreaCovered(Vector3 worldPosition)
    {
        if (_sameAreaCullRadius <= 0f)
            return false;

        float sqrRadius = _sameAreaCullRadius * _sameAreaCullRadius;

        for (int i = _activeNonCriticalPopups.Count - 1; i >= 0; i--)
        {
            WormDamagePopupView activePopup = _activeNonCriticalPopups[i];

            if (activePopup == null || !activePopup.gameObject.activeInHierarchy)
            {
                _activeNonCriticalPopups.RemoveAt(i);
                continue;
            }

            if (activePopup.IsCompleting)
                continue;

            Vector2 delta = (Vector2)(activePopup.SourceWorldPosition - worldPosition);

            if (delta.sqrMagnitude <= sqrRadius)
                return true;
        }

        return false;
    }

    private void AccelerateOldestNonCriticalPopup()
    {
        for (int i = 0; i < _activeNonCriticalPopups.Count; i++)
        {
            WormDamagePopupView popup = _activeNonCriticalPopups[i];

            if (popup != null && popup.TryFastFade())
                return;
        }
    }

    private void RecountNonCriticalPopups()
    {
        _activeNormalCount = 0;
        _activeDamageOverTimeCount = 0;

        for (int i = _activeNonCriticalPopups.Count - 1; i >= 0; i--)
        {
            WormDamagePopupView popup = _activeNonCriticalPopups[i];

            if (popup == null || !popup.gameObject.activeInHierarchy)
            {
                _activeNonCriticalPopups.RemoveAt(i);
                continue;
            }

            if (popup.Kind == DamageKind.DamageOverTime)
                _activeDamageOverTimeCount++;
            else
                _activeNormalCount++;
        }
    }

    private void OnValidate()
    {
        _initialPoolSize = Mathf.Max(0, _initialPoolSize);
        _normalPopupLimit = Mathf.Max(0, _normalPopupLimit);
        _damageOverTimePopupLimit = Mathf.Max(0, _damageOverTimePopupLimit);
        _visiblePopupSoftLimit = Mathf.Max(0, _visiblePopupSoftLimit);
        _fastAnimationThreshold = Mathf.Max(0, _fastAnimationThreshold);
        _accelerateOldNormalThreshold = Mathf.Max(0, _accelerateOldNormalThreshold);
        _sameAreaCullRadius = Mathf.Max(0f, _sameAreaCullRadius);
        _crowdedScaleThreshold = Mathf.Max(0, _crowdedScaleThreshold);
        _veryCrowdedScaleThreshold = Mathf.Max(0, _veryCrowdedScaleThreshold);
        _crowdedScaleMultiplier = Mathf.Clamp(_crowdedScaleMultiplier, 0.1f, 1f);
        _veryCrowdedScaleMultiplier = Mathf.Clamp(_veryCrowdedScaleMultiplier, 0.1f, 1f);

        if (_crowdedScaleThreshold > 0
            && _veryCrowdedScaleThreshold > 0
            && _veryCrowdedScaleThreshold < _crowdedScaleThreshold)
        {
            _veryCrowdedScaleThreshold = _crowdedScaleThreshold;
        }
    }
}
