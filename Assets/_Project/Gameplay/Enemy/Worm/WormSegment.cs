using UnityEngine;

public enum WormSegmentType
{
    None,
    Head,
    Body,
    Tail
}

public sealed class WormSegment : MonoBehaviour
{
    [field: SerializeField] public WormSegmentType Type { get; private set; }
    [field: SerializeField] public Transform VisualRoot { get; private set; }

    [Header("Cocoon Overlay (Visual Only)")]
    [SerializeField] private GameObject _cocoonVisual;

    [Header("Cocoon Shake")]
    [SerializeField, Min(0f)] private float _cocoonShakeInterval = 3f;
    [SerializeField, Min(0f)] private float _cocoonShakeAngle = 10f;

    private WormSegmentCocoonPresenter _cocoonPresenter;
    private WormSegmentDamageBinding _damageBinding;
    private WormSegmentPooledViewLifecycle _pooledViewLifecycle;
    private WormSegmentVisualRig _visualRig;

    public Transform CachedTransform { get; private set; }
    public WormFaceVisualController FaceVisual { get; private set; }
    public WormSection Section { get; internal set; }
    public int Index { get; set; }

    public bool HasCocoon => _cocoonPresenter?.IsVisible == true;
    public bool IsAlive => _pooledViewLifecycle?.IsAlive ?? true;
    public bool HasTailVisualChain => _visualRig?.HasTailVisualChain == true;
    public int TailVisualPartCount => _visualRig?.TailVisualPartCount ?? 0;
    public bool HasHeadFollowChain => _visualRig?.HasHeadFollowChain == true;
    public int HeadFollowPartCount => _visualRig?.HeadFollowPartCount ?? 0;

    private void Awake()
    {
        CachedTransform = transform;
        TryGetComponent(out Collider2D cachedCollider);

        if (Type == WormSegmentType.Head)
            FaceVisual = GetComponentInChildren<WormFaceVisualController>(true);

        SpriteRenderer anchorRenderer = VisualRoot != null
            ? VisualRoot.GetComponentInChildren<SpriteRenderer>()
            : null;

        _cocoonPresenter = new WormSegmentCocoonPresenter(
            CachedTransform,
            _cocoonVisual,
            _cocoonShakeInterval,
            _cocoonShakeAngle);
        _damageBinding = new WormSegmentDamageBinding(gameObject, this);
        _pooledViewLifecycle = new WormSegmentPooledViewLifecycle(
            gameObject,
            VisualRoot,
            cachedCollider);

        _visualRig = new WormSegmentVisualRig(
            Type,
            CachedTransform,
            VisualRoot,
            _cocoonPresenter.VisualTransform,
            anchorRenderer);
    }

    private void OnEnable()
    {
        _cocoonPresenter?.OnOwnerEnabled();
    }

    private void OnDisable()
    {
        _cocoonPresenter?.OnOwnerDisabled();
    }

    private void OnDestroy()
    {
        _cocoonPresenter?.OnOwnerDisabled();
    }

    public void SetSortingOrder(int order)
    {
        _visualRig?.SetSortingOrder(order);
        _cocoonPresenter?.SetSortingOrder(order);
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

        _cocoonPresenter?.Show(rewardProfile, isActiveAndEnabled);
    }

    public void DisableCocoon()
    {
        _cocoonPresenter?.Hide();
    }

    public void Activate()
    {
        _pooledViewLifecycle.Activate();
        DisableCocoon();
        Section = null;
    }

    public void PrepareForWorm()
    {
        Section = null;
        DisableCocoon();
        _pooledViewLifecycle.PrepareForPool();
    }

    public void InitializePresentation(IWormCocoonShakeClock cocoonShakeClock)
    {
        _cocoonPresenter.BindShakeClock(
            cocoonShakeClock,
            isActiveAndEnabled);
    }

    public void UpdateCocoonPresentation()
    {
        _cocoonPresenter?.UpdateOrientation();
    }

    public void BindDamageReceivers(WormCombatController combat)
    {
        _damageBinding.Bind(combat);
    }

    public void SetRuntimeVisible(bool visible)
    {
        _pooledViewLifecycle.SetRuntimeVisible(visible);
    }

    public void KillVisualAndCollision()
    {
        DisableCocoon();
        _pooledViewLifecycle.Kill();
    }

}
