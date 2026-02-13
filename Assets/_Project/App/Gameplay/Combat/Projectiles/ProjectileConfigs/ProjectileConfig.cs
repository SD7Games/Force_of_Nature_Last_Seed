using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Projectile Config")]
public sealed class ProjectileConfig : ScriptableObject
{
    [field: SerializeField] public float Speed { get; private set; } = 8f;
    [field: SerializeField] public int BounceCount { get; private set; } = 0;
    [field: SerializeField] public bool BounceX { get; private set; } = true;
    [field: SerializeField] public bool BounceY { get; private set; } = false;

    [field: SerializeField] public int SplitCount { get; private set; } = 0;
    [field: SerializeField] public float SplitAngle { get; private set; } = 15f;

    [field: SerializeField] public int Penetration { get; private set; } = 0;
    [field: SerializeField] public float Damage { get; private set; } = 1f;

}