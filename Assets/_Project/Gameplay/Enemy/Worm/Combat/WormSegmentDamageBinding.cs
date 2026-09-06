using System;
using UnityEngine;

public sealed class WormSegmentDamageBinding
{
    private readonly GameObject _owner;
    private readonly WormSegment _segment;
    private WormSegmentDamageReceiver[] _receivers =
        Array.Empty<WormSegmentDamageReceiver>();

    public WormSegmentDamageBinding(GameObject owner, WormSegment segment)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _segment = segment ?? throw new ArgumentNullException(nameof(segment));
    }

    public int ReceiverCount => _receivers.Length;

    public void Bind(WormCombatController combat)
    {
        if (combat == null)
            throw new ArgumentNullException(nameof(combat));

        EnsureReceiversCached();

        for (int index = 0; index < _receivers.Length; index++)
            _receivers[index].Initialize(combat, _segment);
    }

    private void EnsureReceiversCached()
    {
        if (_receivers.Length > 0)
            return;

        if (!_owner.TryGetComponent<WormSegmentDamageReceiver>(out _))
            _owner.AddComponent<WormSegmentDamageReceiver>();

        _receivers = _owner.GetComponentsInChildren<WormSegmentDamageReceiver>(true);

        if (_receivers.Length == 0)
            throw new InvalidOperationException(
                $"Worm segment '{_owner.name}' has no damage receiver.");
    }
}
