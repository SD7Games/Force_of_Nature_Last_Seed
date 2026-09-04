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

    private float _maxDamageMultiplier = MaxDamageMultiplier;
    private SalvoProgressionState _salvo = new(DefaultMaxSalvoExtraShots, MaxSalvoExtraShots);
    private CappedBonusState _fireRateBonus = new(DefaultMaxFireRateBonus);
    private CappedBonusState _projectileSpeedBonus = new(DefaultMaxProjectileSpeedBonus);
    private float _maxCriticalChance = MaxCriticalChance;
    private float _maxCriticalDamageMultiplier = MaxCriticalDamageMultiplier;

    public bool IsUnlocked { get; private set; }
    public int BaseDamage { get; private set; } = 1;
    public float DamageMultiplier { get; private set; } = 1f;
    public float FireRateBonus => _fireRateBonus.Value;
    public int SalvoExtraShots => _salvo.ExtraShots;
    public float ProjectileSpeedBonus => _projectileSpeedBonus.Value;
    public float CriticalChance { get; private set; }
    public float CriticalDamageMultiplier { get; private set; } = 2f;
    public float MaxFireRateBonus => _fireRateBonus.Limit;
    public float MaxProjectileSpeedBonus => _projectileSpeedBonus.Limit;

    public bool CanUnlock => !IsUnlocked;

    public void ResetProgression(int baseDamage)
    {
        IsUnlocked = false;
        BaseDamage = Mathf.Max(1, baseDamage);
        DamageMultiplier = 1f;
        _fireRateBonus.Reset();
        _salvo.Reset();
        _projectileSpeedBonus.Reset();
        CriticalChance = 0f;
        CriticalDamageMultiplier = 2f;
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
        _maxDamageMultiplier = Mathf.Clamp(
            maxDamageMultiplier,
            1f,
            MaxDamageMultiplier);

        _fireRateBonus.SetLimit(Mathf.Clamp(
            maxFireRateBonus,
            0f,
            DefaultMaxFireRateBonus));

        _salvo.SetLimit(maxSalvoExtraShots);

        _projectileSpeedBonus.SetLimit(Mathf.Clamp(
            maxProjectileSpeedBonus,
            0f,
            DefaultMaxProjectileSpeedBonus));

        _maxCriticalChance = Mathf.Clamp(
            maxCriticalChance,
            0f,
            MaxCriticalChance);

        _maxCriticalDamageMultiplier = Mathf.Clamp(
            maxCriticalDamageMultiplier,
            1f,
            MaxCriticalDamageMultiplier);

        CriticalDamageMultiplier = Mathf.Clamp(
            Mathf.Max(1f, criticalDamageMultiplier),
            1f,
            _maxCriticalDamageMultiplier);

        DamageMultiplier = Mathf.Min(DamageMultiplier, _maxDamageMultiplier);
        CriticalChance = Mathf.Min(CriticalChance, _maxCriticalChance);
        CriticalDamageMultiplier = Mathf.Min(CriticalDamageMultiplier, _maxCriticalDamageMultiplier);
    }

    public bool CanApplyDamageMultiplier(float multiplier)
    {
        if (!IsUnlocked || multiplier <= 1f)
            return false;

        return DamageMultiplier * multiplier <= _maxDamageMultiplier + FloatEpsilon;
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
        if (!IsUnlocked || chanceBonus <= 0f)
            return false;

        return CriticalChance + chanceBonus <= _maxCriticalChance + FloatEpsilon;
    }

    public bool CanApplyCriticalDamageBonus(float damageBonus)
    {
        if (!IsUnlocked || CriticalChance <= 0f || damageBonus <= 0f)
            return false;

        return CriticalDamageMultiplier + damageBonus <= _maxCriticalDamageMultiplier + FloatEpsilon;
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
        if (multiplier <= 1f)
            return 0f;

        float previousMultiplier = DamageMultiplier;
        DamageMultiplier = Mathf.Min(
            DamageMultiplier * multiplier,
            _maxDamageMultiplier);

        return DamageMultiplier - previousMultiplier;
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
        float accepted = Mathf.Min(
            Mathf.Max(0f, chanceBonus),
            _maxCriticalChance - CriticalChance);

        CriticalChance = Mathf.Clamp(
            CriticalChance + Mathf.Max(0f, accepted),
            0f,
            _maxCriticalChance);

        return accepted;
    }

    public float AddCriticalDamageBonus(float damageBonus)
    {
        float accepted = Mathf.Min(
            Mathf.Max(0f, damageBonus),
            _maxCriticalDamageMultiplier - CriticalDamageMultiplier);

        CriticalDamageMultiplier += Mathf.Max(0f, accepted);
        return accepted;
    }

    public static int ClampDamage(double rawDamage)
    {
        return WeaponDamageClamp.Clamp(rawDamage);
    }

    public AcaciaThornRuntimeState Clone()
    {
        return new AcaciaThornRuntimeState
        {
            _maxDamageMultiplier = _maxDamageMultiplier,
            _salvo = _salvo.Clone(),
            _fireRateBonus = _fireRateBonus.Clone(),
            _projectileSpeedBonus = _projectileSpeedBonus.Clone(),
            _maxCriticalChance = _maxCriticalChance,
            _maxCriticalDamageMultiplier = _maxCriticalDamageMultiplier,
            IsUnlocked = IsUnlocked,
            BaseDamage = BaseDamage,
            DamageMultiplier = DamageMultiplier,
            CriticalChance = CriticalChance,
            CriticalDamageMultiplier = CriticalDamageMultiplier,
        };
    }
}
