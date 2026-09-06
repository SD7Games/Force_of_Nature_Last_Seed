using UnityEngine;

namespace LastSeed.Gameplay.Signals
{
    public readonly struct DamageViewRequest
    {
        public DamageViewRequest(
            int amount,
            Vector3 worldPosition,
            DamageKind kind,
            bool isCritical)
        {
            Amount = amount;
            WorldPosition = worldPosition;
            Kind = kind;
            IsCritical = isCritical;
        }

        public int Amount { get; }
        public Vector3 WorldPosition { get; }
        public DamageKind Kind { get; }
        public bool IsCritical { get; }

        public static DamageViewRequest FromDamageHit(in DamageHit hit)
        {
            return new DamageViewRequest(
                hit.Damage.Amount,
                hit.HitPosition,
                hit.Damage.Kind,
                hit.Damage.IsCritical);
        }
    }
}
