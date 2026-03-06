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
    public WormSection Section { get; set; }

    [field: SerializeField] public WormSegmentType Type { get; private set; }
    [field: SerializeField] public Transform VisualRoot { get; private set; }

    [SerializeField] private bool _hasReward = true;

    public bool HasReward => _hasReward;

    public bool IsAlive { get; private set; } = true;

    public void Activate()
    {
        gameObject.SetActive(true);

        IsAlive = true;

        if (VisualRoot != null)
            VisualRoot.gameObject.SetActive(true);

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;
    }

    public void KillVisualAndCollision()
    {
        IsAlive = false;

        if (VisualRoot != null)
            VisualRoot.gameObject.SetActive(false);

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }
}