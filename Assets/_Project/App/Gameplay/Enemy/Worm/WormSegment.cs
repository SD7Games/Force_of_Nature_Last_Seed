using UnityEngine;

public enum WormSegmentType
{
    Head,
    Body,
    Cocoon,
    Tail
}

[DisallowMultipleComponent]
public sealed class WormSegment : MonoBehaviour
{
    [field: SerializeField] public WormSegmentType Type { get; private set; }

    [field: SerializeField] public Transform VisualRoot { get; private set; }

    [field: SerializeField] public float RotationOffset { get; private set; }

    [SerializeField] private EnemyRailMover _mover;

    private WormController _controller;

    public void AssignController(WormController controller)
    {
        _controller = controller;
    }

    public void StartMove(Transform[] waypoints, float speed)
    {
        gameObject.SetActive(true);

        if (Type == WormSegmentType.Head)
        {
            _mover.Initialize(waypoints, speed);
        }
        else
        {
            _mover.enabled = false;
        }
    }

    public void Die()
    {
        _controller?.RemoveSegment(this);
        gameObject.SetActive(false);
    }
}