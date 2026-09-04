using System;
using UnityEngine;

public sealed class AcaciaThornRuntimeState
{
    private const float FloatEpsilon = 0.0001f;

    public const float DefaultMaxFireRateBonus = 3f;
    public const float DefaultMaxProjectileSpeedBonus = 2f;
    public const float MaxDamageMultiplier = 100000f;
    public const int DefaultMaxSalvoShots = 4;
    public const int MaxSalvoShots = 6;
    public const int DefaultMaxSalvoExtraShots = DefaultMaxSalvoShots - 1;
    public const int MaxSalvoExtraShots = MaxSalvoShots - 1;
    public const float MaxCriticalChance = 1f;
    public const float MaxCriticalDamageMultiplier = 100f;

    private SalvoProgressionState _salvo = new(DefaultMaxSalvoExtraShots, MaxSalvoExtraShots);
    private CappedBonusState _fireRateBonus = new(DefaultMaxFireRateBonus);
    private CappedBonusState _projectileSpeedBonus = new(DefaultMaxProjectileSpeedBonus);
    private CriticalHitProgressionState _criticalHit = new(
        MaxCriticalChance,
        MaxCriticalDamageMultiplier);
    private DamageMultiplierProgressionState _damageMultiplier = new(MaxDamageMultiplier);

    public bool IsUnlocked { get; private set; }
    public int BaseDamage { get; private set; } = 1;
    public float DamageMultiplier => _damageMultiplier.Value;
    public float FireRateBonus => _fireRateBonus.Value;
    public int SalvoExtraShots => _salvo.ExtraShots;
    public float ProjectileSpeedBonus => _projectileSpeedBonus.Value;
    public float CriticalChance => _criticalHit.Chance;
    public float CriticalDamageMultiplier => _criticalHit.DamageMultiplier;
    public float MaxFireRateBonus => _fireRateBonus.Limit;
    public float MaxProjectileSpeedBonus => _projectileSpeedBonus.Limit;

    public bool CanUnlock => !IsUnlocked;

    public void ResetProgression(int baseDamage)
    {
        IsUnlocked = false;
        BaseDamage = Mathf.Max(1, baseDamage);
        _damageMultiplier.Reset();
        _fireRateBonus.Reset();
        _salvo.Reset();
        _projectileSpeedBonus.Reset();
        _criticalHit.Reset();
    }

    public void SetProgressionLimits(
        float maxDamageMultiplier,
        float maxFireRateBonus,
        int maxSalvoExtraShots,
        float maxProjectileSpeedBonus,
        float maxCriticalChance,
        float criticalDamageMultiplier,
        float maxCriticalDamageMultiplier)
    {
        _damageMultiplier.SetLimit(Mathf.Clamp(
            maxDamageMultiplier,
            1f,
            MaxDamageMultiplier));

        _fireRateBonus.SetLimit(Mathf.Clamp(
            maxFireRateBonus,
            0f,
            DefaultMaxFireRateBonus));

        _salvo.SetLimit(maxSalvoExtraShots);

        _projectileSpeedBonus.SetLimit(Mathf.Clamp(
            maxProjectileSpeedBonus,
            0f,
            DefaultMaxProjectileSpeedBonus));

        _criticalHit.SetLimits(
            Mathf.Clamp(maxCriticalChance, 0f, MaxCriticalChance),
            Mathf.Clamp(
                maxCriticalDamageMultiplier,
                1f,
                MaxCriticalDamageMultiplier));
        _criticalHit.SetDamageMultiplier(criticalDamageMultiplier);

    }

    public bool CanApplyDamageMultiplier(float multiplier)
    {
        return IsUnlocked && _damageMultiplier.CanApply(multiplier);
    }

    public bool CanApplyFireRateBonus(float bonus)
    {
        return IsUnlocked && _fireRateBonus.CanApply(bonus);
    }

    public bool CanApplySalvoShots(int extraShots)
    {
        return IsUnlocked && _salvo.CanApply(extraShots);
    }

    public bool CanApplySalvoShots(
        int extraShots,
        int maxSalvoExtraShotsAfterApply)
    {
        return IsUnlocked && _salvo.CanApply(extraShots, maxSalvoExtraShotsAfterApply);
    }

    public bool CanApplyProjectileSpeedBonus(float bonus)
    {
        return IsUnlocked && _projectileSpeedBonus.CanApply(bonus);
    }

    public bool CanApplyCriticalChance(float chanceBonus)
    {
        return IsUnlocked && _criticalHit.CanApplyChance(chanceBonus);
    }

    public bool CanApplyCriticalDamageBonus(float damageBonus)
    {
        return IsUnlocked
            && CriticalChance > 0f
            && _criticalHit.CanApplyDamage(damageBonus);
    }

    public void Unlock(int baseDamage)
    {
        SetBaseDamage(baseDamage);
        IsUnlocked = true;
    }

    public void SetBaseDamage(int baseDamage)
    {
        BaseDamage = Mathf.Max(BaseDamage, Mathf.Max(1, baseDamage));
    }

    public float ApplyDamageMultiplier(float multiplier)
    {
        return _damageMultiplier.Apply(multiplier);
    }

    public float AddFireRateBonus(float bonus)
    {
        return _fireRateBonus.Add(bonus);
    }

    public int AddSalvoShots(int extraShots)
    {
        return _salvo.Add(extraShots);
    }

    public void ExpandSalvoExtraShotLimit(int maxSalvoExtraShots)
    {
        _salvo.ExpandLimit(maxSalvoExtraShots);
    }

    public float AddProjectileSpeedBonus(float bonus)
    {
        return _projectileSpeedBonus.Add(bonus);
    }

    public float AddCriticalChance(float chanceBonus)
    {
        return _criticalHit.AddChance(chanceBonus);
    }

    public float AddCriticalDamageBonus(float damageBonus)
    {
        return _criticalHit.AddDamage(damageBonus);
    }

    public static int ClampDamage(double rawDamage)
    {
        return WeaponDamageClamp.Clamp(rawDamage);
    }

    public AcaciaThornRuntimeState Clone()
    {
        return new AcaciaThornRuntimeState
        {
            _damageMultiplier = _damageMultiplier.Clone(),
            _salvo = _salvo.Clone(),
            _fireRateBonus = _fireRateBonus.Clone(),
            _projectileSpeedBonus = _projectileSpeedBonus.Clone(),
            _criticalHit = _criticalHit.Clone(),
            IsUnlocked = IsUnlocked,
            BaseDamage = BaseDamage,
        };
    }
}
