using System;
using UnityEngine;

public enum WormSegmentType
{
    None,
    Head,
    Body,
    Tail
}

/// <summary>
/// Represents a single worm segment instance.
/// Handles rendering setup, cocoon overlay logic,
/// collision state and lifecycle transitions.
/// </summary>
public sealed class WormSegment : MonoBehaviour
{
    [field: SerializeField] public WormSegmentType Type { get; private set; }
    [field: SerializeField] public Transform VisualRoot { get; private set; }

    [Header("Cocoon Overlay (Visual Only)")]
    [SerializeField] private GameObject _cocoonVisual;

    [Header("Cocoon Shake")]
    [SerializeField, Min(0f)] private float _cocoonShakeInterval = 3f;
    [SerializeField, Min(0f)] private float _cocoonShakeAngle = 10f;

    private Collider2D _cachedCollider;
    private SpriteRenderer _cocoonRenderer;
    private CocoonVisualController _cocoonVisualController;
    private WormSegmentDamageReceiver[] _damageReceivers =
        Array.Empty<WormSegmentDamageReceiver>();

    private Transform _cocoonTransform;
    private WormSegmentVisualRig _visualRig;
    private IWormCocoonShakeClock _cocoonShakeClock;
    private bool _usesSyncedCocoonShake;

    public Transform CachedTransform { get; private set; }
    public WormFaceVisualController FaceVisual { get; private set; }
    public WormSection Section { get; internal set; }
    public int Index { get; set; }

    public bool HasCocoon { get; private set; }
    public bool IsAlive { get; private set; } = true;
    public bool HasTailVisualChain => _visualRig?.HasTailVisualChain == true;
    public int TailVisualPartCount => _visualRig?.TailVisualPartCount ?? 0;
    public bool HasHeadFollowChain => _visualRig?.HasHeadFollowChain == true;
    public int HeadFollowPartCount => _visualRig?.HeadFollowPartCount ?? 0;

    private void Awake()
    {
        CachedTransform = transform;
        _cachedCollider = GetComponent<Collider2D>();

        if (Type == WormSegmentType.Head)
            FaceVisual = GetComponentInChildren<WormFaceVisualController>(true);

        SpriteRenderer anchorRenderer = VisualRoot != null
            ? VisualRoot.GetComponentInChildren<SpriteRenderer>()
            : null;

        if (_cocoonVisual != null)
        {
            _cocoonRenderer = _cocoonVisual.GetComponentInChildren<SpriteRenderer>(true);
            _cocoonVisualController = _cocoonVisual.GetComponentInChildren<CocoonVisualController>(true);
            _cocoonTransform = _cocoonVisual.transform;
        }

        _visualRig = new WormSegmentVisualRig(
            Type,
            CachedTransform,
            VisualRoot,
            _cocoonTransform,
            anchorRenderer);
    }

    private void OnEnable()
    {
        if (!HasCocoon)
            return;

        RegisterSyncedCocoonShake();
    }

    private void OnDisable()
    {
        UnregisterSyncedCocoonShake();
    }

    private void OnDestroy()
    {
        UnregisterSyncedCocoonShake();
    }

    private void LateUpdate()
    {
        if (HasCocoon && _cocoonTransform != null)
        {
            float shakeOffset = _usesSyncedCocoonShake
                ? _cocoonShakeClock.RotationOffset
                : 0f;

            _cocoonTransform.localEulerAngles =
                new Vector3(0f, 0f, -transform.eulerAngles.z + shakeOffset);
        }
    }

    public void SetSortingOrder(int order)
    {
        _visualRig?.SetSortingOrder(order);

        if (_cocoonRenderer != null)
        {
            _cocoonRenderer.sortingOrder = order + 100;
            _cocoonVisualController?.SetEffectSorting(
                _cocoonRenderer.sortingLayerID,
                _cocoonRenderer.sortingOrder + 1);
        }
    }

    public void ResetTailVisualRootRotation()
    {
        _visualRig?.ResetTailVisualRootRotation();
    }

    public void SetTailVisualPartPose(int index, Vector3 position, float angle)
    {
        _visualRig?.SetTailVisualPartPose(index, position, angle);
    }

    public void SetHeadFollowPartPose(int index, Vector3 position, float angle)
    {
        _visualRig?.SetHeadFollowPartPose(index, position, angle);
    }

    public void SetHeadFollowChainVisible(bool visible)
    {
        _visualRig?.SetHeadFollowChainVisible(visible);
    }

    public bool TryGetLastHeadFollowPartPosition(out Vector3 position)
    {
        if (_visualRig != null)
            return _visualRig.TryGetLastHeadFollowPartPosition(out position);

        position = default;
        return false;
    }

    public void EnableCocoon()
    {
        EnableCocoon(null);
    }

    public void EnableCocoon(CocoonRewardProfile rewardProfile)
    {
        if (Type != WormSegmentType.Body)
            return;

        HasCocoon = true;

        if (_cocoonVisual != null)
            _cocoonVisual.SetActive(true);

        if (_cocoonVisualController != null)
            _cocoonVisualController.Apply(rewardProfile);
        else if (_cocoonRenderer != null)
            _cocoonRenderer.color = Color.white;

        if (isActiveAndEnabled)
            RegisterSyncedCocoonShake();
    }

    public void DisableCocoon()
    {
        UnregisterSyncedCocoonShake();
        HasCocoon = false;

        if (_cocoonRenderer != null)
            _cocoonRenderer.color = Color.white;

        if (_cocoonVisualController != null)
            _cocoonVisualController.ResetVisual();

        if (_cocoonVisual != null)
            _cocoonVisual.SetActive(false);
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        IsAlive = true;

        if (VisualRoot != null && !VisualRoot.gameObject.activeSelf)
            VisualRoot.gameObject.SetActive(true);

        if (_cachedCollider != null && !_cachedCollider.enabled)
            _cachedCollider.enabled = true;

        DisableCocoon();
        Section = null;
    }

    public void PrepareForWorm()
    {
        IsAlive = true;
        Section = null;

        if (VisualRoot != null && !VisualRoot.gameObject.activeSelf)
            VisualRoot.gameObject.SetActive(true);

        if (_cachedCollider != null && !_cachedCollider.enabled)
            _cachedCollider.enabled = true;

        DisableCocoon();

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    public void InitializePresentation(IWormCocoonShakeClock cocoonShakeClock)
    {
        _cocoonShakeClock = cocoonShakeClock ??
            throw new ArgumentNullException(nameof(cocoonShakeClock));
    }

    public void BindDamageReceivers(WormCombatController combat)
    {
        if (combat == null)
            throw new ArgumentNullException(nameof(combat));

        EnsureDamageReceiversCached();

        for (int receiverIndex = 0; receiverIndex < _damageReceivers.Length; receiverIndex++)
            _damageReceivers[receiverIndex].Initialize(combat, this);
    }

    public void SetRuntimeVisible(bool visible)
    {
        if (!IsAlive)
            return;

        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);

        if (!visible)
            return;

        if (VisualRoot != null && !VisualRoot.gameObject.activeSelf)
            VisualRoot.gameObject.SetActive(true);

        if (_cachedCollider != null && !_cachedCollider.enabled)
            _cachedCollider.enabled = true;
    }

    public void KillVisualAndCollision()
    {
        IsAlive = false;

        if (VisualRoot != null && VisualRoot.gameObject.activeSelf)
            VisualRoot.gameObject.SetActive(false);

        if (_cachedCollider != null && _cachedCollider.enabled)
            _cachedCollider.enabled = false;

        DisableCocoon();

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void EnsureDamageReceiversCached()
    {
        if (_damageReceivers.Length > 0)
            return;

        if (!TryGetComponent<WormSegmentDamageReceiver>(out _))
            gameObject.AddComponent<WormSegmentDamageReceiver>();

        _damageReceivers = GetComponentsInChildren<WormSegmentDamageReceiver>(true);

        if (_damageReceivers.Length == 0)
            throw new InvalidOperationException($"Worm segment '{name}' has no damage receiver.");
    }

    private void RegisterSyncedCocoonShake()
    {
        if (_usesSyncedCocoonShake)
            return;

        if (_cocoonTransform == null ||
            _cocoonShakeClock == null ||
            _cocoonShakeAngle <= 0f)
            return;

        _usesSyncedCocoonShake = true;
        _cocoonShakeClock.Register(_cocoonShakeInterval, _cocoonShakeAngle);
    }

    private void UnregisterSyncedCocoonShake()
    {
        if (!_usesSyncedCocoonShake)
            return;

        _usesSyncedCocoonShake = false;
        _cocoonShakeClock?.Unregister();
    }
}
