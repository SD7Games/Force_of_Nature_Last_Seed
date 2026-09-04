using System;
using UnityEngine;

public sealed class WeaponRuntimeState
{
    private const float FloatEpsilon = 0.0001f;

    public const int DefaultMaxParallelProjectiles = 5;
    public const int MaxParallelProjectiles = 8;
    public const int DefaultMaxSalvoShots = 4;
    public const int MaxSalvoShots = 6;
    public const int DefaultMaxSalvoExtraShots = DefaultMaxSalvoShots - 1;
    public const int MaxSalvoExtraShots = MaxSalvoShots - 1;
    public const int MaxProjectileDamage = WeaponDamageClamp.MaximumDamage;
    public const float DefaultMaxFireRateBonus = 3f;
    public const float DefaultMaxProjectileSpeedBonus = 2f;
    public const float MaxDamageMultiplier = 100000f;
    public const float MaxCriticalDamageMultiplier = 100f;
    public const int MaxPenetrationBonus = 5;
    public const float MaxCriticalChance = 1f;

    private WeaponShotPatternState _shotPattern = new();
    private CappedBonusState _fireRateBonus = new(DefaultMaxFireRateBonus);
    private CappedBonusState _projectileSpeedBonus = new(DefaultMaxProjectileSpeedBonus);
    private CriticalHitProgressionState _criticalHit = new(
        MaxCriticalChance,
        MaxCriticalDamageMultiplier);
    private DamageMultiplierProgressionState _damageMultiplier = new(MaxDamageMultiplier);

    private int _maxPenetrationBonus = MaxPenetrationBonus;

    public float DamageMultiplier => _damageMultiplier.Value;
    public float FireRateBonus => _fireRateBonus.Value;
    public float CriticalChance => _criticalHit.Chance;
    public float CriticalDamageMultiplier => _criticalHit.DamageMultiplier;
    public int PenetrationBonus { get; private set; }
    public int ParallelProjectileCount => _shotPattern.ParallelProjectileCount;
    public float ParallelSpacing => _shotPattern.ParallelSpacing;
    public int SalvoExtraShots => _shotPattern.SalvoExtraShots;
    public float SalvoInterval => _shotPattern.SalvoInterval;
    public float ProjectileSpeedBonus => _projectileSpeedBonus.Value;
    public float MaxFireRateBonus => _fireRateBonus.Limit;
    public float MaxProjectileSpeedBonus => _projectileSpeedBonus.Limit;
    public System.Collections.Generic.IReadOnlyList<ShotModifierData> ShotModifiers =>
        _shotPattern.Modifiers;

    public bool CanAddDamageMultiplier => _damageMultiplier.CanAdd;
    public bool CanAddFireRateBonus => _fireRateBonus.CanAdd;
    public bool CanAddCriticalChance => _criticalHit.CanAddChance;
    public bool CanAddCriticalDamage => _criticalHit.CanAddDamage;
    public bool CanAddPenetration => PenetrationBonus < _maxPenetrationBonus;
    public bool CanAddParallelProjectiles => _shotPattern.CanAddParallelProjectiles;
    public bool CanAddSalvoShots => _shotPattern.CanAddSalvoShots;
    public bool CanAddProjectileSpeedBonus => _projectileSpeedBonus.CanAdd;

    public void ResetProgression()
    {
        _shotPattern.Reset();
        _damageMultiplier.Reset();
        _fireRateBonus.Reset();
        _criticalHit.Reset();
        PenetrationBonus = 0;
        _projectileSpeedBonus.Reset();
    }

    public bool CanApplyDamageMultiplier(float multiplier)
    {
        return _damageMultiplier.CanApply(multiplier);
    }

    public bool CanApplyFireRateBonus(float bonus)
    {
        return _fireRateBonus.CanApply(bonus);
    }

    public bool CanApplyProjectileSpeedBonus(float bonus)
    {
        return _projectileSpeedBonus.CanApply(bonus);
    }

    public bool CanApplyCriticalChance(float chanceBonus)
    {
        return _criticalHit.CanApplyChance(chanceBonus);
    }

    public bool CanApplyCriticalDamageBonus(float damageBonus)
    {
        return _criticalHit.CanApplyDamage(damageBonus);
    }

    public bool CanApplyPenetrationBonus(int bonus)
    {
        if (bonus <= 0)
            return false;

        return PenetrationBonus + bonus <= _maxPenetrationBonus;
    }

    public bool CanApplyParallelProjectiles(int bonusProjectiles)
    {
        return _shotPattern.CanApplyParallelProjectiles(bonusProjectiles);
    }

    public bool CanApplySalvoShots(int extraShots)
    {
        return _shotPattern.CanApplySalvoShots(extraShots);
    }

    public bool CanApplyParallelProjectiles(
        int bonusProjectiles,
        int maxParallelProjectilesAfterApply)
    {
        return _shotPattern.CanApplyParallelProjectiles(
            bonusProjectiles,
            maxParallelProjectilesAfterApply);
    }

    public bool CanApplySalvoShots(
        int extraShots,
        int maxSalvoExtraShotsAfterApply)
    {
        return _shotPattern.CanApplySalvoShots(extraShots, maxSalvoExtraShotsAfterApply);
    }

    public float ApplyDamageMultiplier(float multiplier)
    {
        return _damageMultiplier.Apply(multiplier);
    }

    public void SetProgressionLimits(
        float maxDamageMultiplier,
        float maxCriticalChance,
        float maxCriticalDamageMultiplier,
        int maxPenetrationBonus,
        int maxParallelProjectiles,
        int maxSalvoExtraShots)
    {
        _damageMultiplier.SetLimit(UnityEngine.Mathf.Clamp(
            maxDamageMultiplier,
            1f,
            MaxDamageMultiplier));

        _criticalHit.SetLimits(
            UnityEngine.Mathf.Clamp(maxCriticalChance, 0f, MaxCriticalChance),
            UnityEngine.Mathf.Clamp(
                maxCriticalDamageMultiplier,
                1f,
                MaxCriticalDamageMultiplier));

        _maxPenetrationBonus = UnityEngine.Mathf.Clamp(
            maxPenetrationBonus,
            0,
            MaxPenetrationBonus);

        _shotPattern.SetLimits(maxParallelProjectiles, maxSalvoExtraShots);

        PenetrationBonus = UnityEngine.Mathf.Min(PenetrationBonus, _maxPenetrationBonus);
    }

    public void SetFireRateBonusLimit(float maxFireRateBonus)
    {
        _fireRateBonus.SetLimit(maxFireRateBonus);
    }

    public void SetProjectileSpeedBonusLimit(float maxProjectileSpeedBonus)
    {
        _projectileSpeedBonus.SetLimit(maxProjectileSpeedBonus);
    }

    public float AddFireRateBonus(float bonus)
    {
        return _fireRateBonus.Add(bonus);
    }

    public float AddProjectileSpeedBonus(float bonus)
    {
        return _projectileSpeedBonus.Add(bonus);
    }

    public float AddCriticalChance(float chanceBonus, float minimumCriticalDamageMultiplier)
    {
        return _criticalHit.AddChance(chanceBonus, minimumCriticalDamageMultiplier);
    }

    public float AddCriticalDamageBonus(float damageBonus)
    {
        return _criticalHit.AddDamage(damageBonus);
    }

    public int AddPenetration(int bonus)
    {
        int accepted = UnityEngine.Mathf.Min(
            UnityEngine.Mathf.Max(0, bonus),
            _maxPenetrationBonus - PenetrationBonus);

        PenetrationBonus += UnityEngine.Mathf.Max(0, accepted);
        return accepted;
    }

    public int AddSalvoShots(int extraShots, float interval)
    {
        return _shotPattern.AddSalvoShots(extraShots, interval);
    }

    public void ExpandParallelProjectileLimit(int maxParallelProjectiles)
    {
        _shotPattern.ExpandParallelLimit(maxParallelProjectiles);
    }

    public void ExpandSalvoExtraShotLimit(int maxSalvoExtraShots)
    {
        _shotPattern.ExpandSalvoLimit(maxSalvoExtraShots);
    }

    public bool AddShotModifier(ShotModifierData modifier)
    {
        return _shotPattern.AddModifier(modifier);
    }

    public bool CanAddShotModifier(ShotModifierData modifier)
    {
        return _shotPattern.CanAddModifier(modifier);
    }

    public int AddParallelProjectiles(int bonusProjectiles, float spacing)
    {
        return _shotPattern.AddParallelProjectiles(bonusProjectiles, spacing);
    }

    public static int ClampDamage(double rawDamage)
    {
        return WeaponDamageClamp.Clamp(rawDamage);
    }

    public WeaponRuntimeState Clone()
    {
        WeaponRuntimeState clone = new()
        {
            _damageMultiplier = _damageMultiplier.Clone(),
            _maxPenetrationBonus = _maxPenetrationBonus,
            _criticalHit = _criticalHit.Clone(),
            _shotPattern = _shotPattern.Clone(),
            _fireRateBonus = _fireRateBonus.Clone(),
            _projectileSpeedBonus = _projectileSpeedBonus.Clone(),
            PenetrationBonus = PenetrationBonus,
        };

        return clone;
    }
}
