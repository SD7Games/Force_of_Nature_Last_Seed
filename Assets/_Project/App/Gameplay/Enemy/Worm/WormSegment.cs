using UnityEngine;

public enum WormSegmentType
{
    Head,
    Body,
    Cocoon,
    Tail
}

public sealed class WormSegment : MonoBehaviour
{
    [field: SerializeField] public WormSegmentType Type { get; private set; }
    [field: SerializeField] public Transform VisualRoot { get; private set; }

    [SerializeField] private bool _hasReward = true;

    private Collider2D _cachedCollider;

    public Transform CachedTransform { get; private set; }
    public WormSection Section { get; set; }
    public bool HasReward => _hasReward;
    public bool IsAlive { get; private set; } = true;

    private void Awake()
    {
        CachedTransform = transform;
        _cachedCollider = GetComponent<Collider2D>();
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        IsAlive = true;

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
    }
}