using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormSegmentDamageReceiver : MonoBehaviour, IDamageable
{
    private WormCombatController _combat;
    private WormSegment _segment;

    public void Initialize(WormCombatController combat, WormSegment segment)
    {
        _combat = combat;
        _segment = segment;
    }

    public void TakeDamage(int damage)
    {
        if (_combat == null || _segment == null)
            return;

        if (!_segment.IsAlive)
            return;

        _combat.RegisterHit(_segment, damage);
    }
}