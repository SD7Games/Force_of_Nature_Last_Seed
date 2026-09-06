using System;
using UnityEngine;

public sealed class WormSegmentCocoonPresenter
{
    private const int CocoonSortingOffset = 100;
    private const int EffectSortingOffset = 1;

    private readonly Transform _ownerTransform;
    private readonly GameObject _visual;
    private readonly Transform _visualTransform;
    private readonly SpriteRenderer _renderer;
    private readonly CocoonVisualController _visualController;
    private readonly float _shakeInterval;
    private readonly float _shakeAngle;

    private IWormCocoonShakeClock _shakeClock;
    private bool _isShakeRegistered;

    public WormSegmentCocoonPresenter(
        Transform ownerTransform,
        GameObject visual,
        float shakeInterval,
        float shakeAngle)
    {
        _ownerTransform = ownerTransform ??
            throw new ArgumentNullException(nameof(ownerTransform));
        _visual = visual;
        _visualTransform = visual != null ? visual.transform : null;
        _renderer = visual != null
            ? visual.GetComponentInChildren<SpriteRenderer>(true)
            : null;
        _visualController = visual != null
            ? visual.GetComponentInChildren<CocoonVisualController>(true)
            : null;
        _shakeInterval = Mathf.Max(0f, shakeInterval);
        _shakeAngle = Mathf.Max(0f, shakeAngle);
    }

    public bool IsVisible { get; private set; }
    public Transform VisualTransform => _visualTransform;

    public void BindShakeClock(
        IWormCocoonShakeClock shakeClock,
        bool ownerIsActive)
    {
        if (shakeClock == null)
            throw new ArgumentNullException(nameof(shakeClock));

        UnregisterShake();
        _shakeClock = shakeClock;

        if (ownerIsActive && IsVisible)
            RegisterShake();
    }

    public void Show(CocoonRewardProfile rewardProfile, bool ownerIsActive)
    {
        IsVisible = true;

        if (_visual != null && !_visual.activeSelf)
            _visual.SetActive(true);

        if (_visualController != null)
            _visualController.Apply(rewardProfile);
        else if (_renderer != null)
            _renderer.color = Color.white;

        if (ownerIsActive)
            RegisterShake();
    }

    public void Hide()
    {
        UnregisterShake();
        IsVisible = false;

        if (_renderer != null)
            _renderer.color = Color.white;

        _visualController?.ResetVisual();

        if (_visual != null && _visual.activeSelf)
            _visual.SetActive(false);
    }

    public void OnOwnerEnabled()
    {
        if (IsVisible)
            RegisterShake();
    }

    public void OnOwnerDisabled()
    {
        UnregisterShake();
    }

    public void UpdateOrientation()
    {
        if (!IsVisible || _visualTransform == null)
            return;

        float shakeOffset = _isShakeRegistered
            ? _shakeClock.RotationOffset
            : 0f;

        _visualTransform.localEulerAngles = new Vector3(
            0f,
            0f,
            -_ownerTransform.eulerAngles.z + shakeOffset);
    }

    public void SetSortingOrder(int segmentSortingOrder)
    {
        if (_renderer == null)
            return;

        _renderer.sortingOrder = segmentSortingOrder + CocoonSortingOffset;
        _visualController?.SetEffectSorting(
            _renderer.sortingLayerID,
            _renderer.sortingOrder + EffectSortingOffset);
    }

    private void RegisterShake()
    {
        if (_isShakeRegistered ||
            _visualTransform == null ||
            _shakeClock == null ||
            _shakeAngle <= 0f)
        {
            return;
        }

        _isShakeRegistered = true;
        _shakeClock.Register(_shakeInterval, _shakeAngle);
    }

    private void UnregisterShake()
    {
        if (!_isShakeRegistered)
            return;

        _isShakeRegistered = false;
        _shakeClock?.Unregister();
    }
}
