using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Weapon Config")]
public sealed class WeaponConfig : ScriptableObject
{
    [Header("Base")]
    public float FireRate = 1.4f;

    [Header("Projectile")]
    public ProjectileConfig Projectile;

    [Header("Modifiers")]
    public List<ShotModifierData> Modifiers = new();
}