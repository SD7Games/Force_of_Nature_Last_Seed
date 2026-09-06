using System;
using UnityEngine;

public sealed class WormSegmentPooledViewLifecycle
{
    private readonly GameObject _owner;
    private readonly GameObject _visualRoot;
    private readonly Collider2D _collider;

    public WormSegmentPooledViewLifecycle(
        GameObject owner,
        Transform visualRoot,
        Collider2D collider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _visualRoot = visualRoot != null ? visualRoot.gameObject : null;
        _collider = collider;
    }

    public bool IsAlive { get; private set; } = true;

    public void Activate()
    {
        SetOwnerActive(true);
        IsAlive = true;
        SetPresentationActive(true);
    }

    public void PrepareForPool()
    {
        IsAlive = true;
        SetPresentationActive(true);
        SetOwnerActive(false);
    }

    public void SetRuntimeVisible(bool visible)
    {
        if (!IsAlive)
            return;

        SetOwnerActive(visible);

        if (visible)
            SetPresentationActive(true);
    }

    public void Kill()
    {
        IsAlive = false;
        SetPresentationActive(false);
        SetOwnerActive(false);
    }

    private void SetPresentationActive(bool active)
    {
        if (_visualRoot != null && _visualRoot.activeSelf != active)
            _visualRoot.SetActive(active);

        if (_collider != null && _collider.enabled != active)
            _collider.enabled = active;
    }

    private void SetOwnerActive(bool active)
    {
        if (_owner.activeSelf != active)
            _owner.SetActive(active);
    }
}
