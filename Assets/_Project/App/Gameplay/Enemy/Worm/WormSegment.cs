using System;
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

    [SerializeField] private EnemyRailMover _mover;

    private Action<WormSegment> _returnToPool;

    public void SetupReturn(Action<WormSegment> returnToPool)
    {
        _returnToPool = returnToPool;

        if (_mover != null)
        {
            _mover.Completed -= OnCompleted;
            _mover.Completed += OnCompleted;
        }
    }

    public void StartMove(Transform[] waypoints, float speed)
    {
        gameObject.SetActive(true);

        if (_mover == null)
            _mover = GetComponent<EnemyRailMover>();

        _mover.Initialize(waypoints, speed);
    }

    private void OnCompleted()
    {
        _returnToPool?.Invoke(this);
    }
}