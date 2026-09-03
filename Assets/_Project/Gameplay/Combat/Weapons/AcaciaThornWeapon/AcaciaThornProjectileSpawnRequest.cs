using UnityEngine;

public readonly struct AcaciaThornProjectileSpawnRequest
{
    public AcaciaThornProjectileSpawnRequest(
        Vector3 position,
        Vector2 direction,
        int damage,
        DamageKind damageKind,
        bool isCritical,
        float speed,
        float lifeTime,
        int bounces,
        int splitCount,
        bool canSplit)
    {
        Position = position;
        Direction = direction;
        Damage = damage;
        DamageKind = damageKind;
        IsCritical = isCritical;
        Speed = speed;
        LifeTime = lifeTime;
        Bounces = bounces;
        SplitCount = splitCount;
        CanSplit = canSplit;
    }

    public Vector3 Position { get; }
    public Vector2 Direction { get; }
    public int Damage { get; }
    public DamageKind DamageKind { get; }
    public bool IsCritical { get; }
    public float Speed { get; }
    public float LifeTime { get; }
    public int Bounces { get; }
    public int SplitCount { get; }
    public bool CanSplit { get; }
}
