using System;

public sealed class CriticalHitProgressionState
{
    private const float ComparisonEpsilon = 0.0001f;
    private const float MinimumDamageMultiplier = 1f;

    private float _maxChance;
    private float _maxDamageMultiplier;

    public CriticalHitProgressionState(
        float maxChance,
        float maxDamageMultiplier,
        float initialDamageMultiplier = 2f)
    {
        _maxChance = Math.Max(0f, maxChance);
        _maxDamageMultiplier = Math.Max(MinimumDamageMultiplier, maxDamageMultiplier);
        SetDamageMultiplier(initialDamageMultiplier);
    }

    private CriticalHitProgressionState(
        float chance,
        float damageMultiplier,
        float maxChance,
        float maxDamageMultiplier)
    {
        Chance = chance;
        DamageMultiplier = damageMultiplier;
        _maxChance = maxChance;
        _maxDamageMultiplier = maxDamageMultiplier;
    }

    public float Chance { get; private set; }
    public float DamageMultiplier { get; private set; }
    public bool CanAddChance => Chance < _maxChance;
    public bool CanAddDamage => DamageMultiplier < _maxDamageMultiplier;

    public void Reset(float damageMultiplier = 2f)
    {
        Chance = 0f;
        SetDamageMultiplier(damageMultiplier);
    }

    public void SetLimits(float maxChance, float maxDamageMultiplier)
    {
        _maxChance = Math.Max(0f, maxChance);
        _maxDamageMultiplier = Math.Max(MinimumDamageMultiplier, maxDamageMultiplier);
        Chance = Math.Min(Chance, _maxChance);
        DamageMultiplier = Math.Min(DamageMultiplier, _maxDamageMultiplier);
    }

    public void SetDamageMultiplier(float damageMultiplier)
    {
        DamageMultiplier = Clamp(
            damageMultiplier,
            MinimumDamageMultiplier,
            _maxDamageMultiplier);
    }

    public bool CanApplyChance(float bonus)
    {
        return bonus > 0f && Chance + bonus <= _maxChance + ComparisonEpsilon;
    }

    public bool CanApplyDamage(float bonus)
    {
        return bonus > 0f
            && DamageMultiplier + bonus <= _maxDamageMultiplier + ComparisonEpsilon;
    }

    public float AddChance(float bonus, float minimumDamageMultiplier = MinimumDamageMultiplier)
    {
        float accepted = Math.Min(Math.Max(0f, bonus), _maxChance - Chance);
        Chance = Clamp(Chance + Math.Max(0f, accepted), 0f, _maxChance);
        SetDamageMultiplier(Math.Max(DamageMultiplier, minimumDamageMultiplier));
        return accepted;
    }

    public float AddDamage(float bonus)
    {
        float accepted = Math.Min(
            Math.Max(0f, bonus),
            _maxDamageMultiplier - DamageMultiplier);
        DamageMultiplier += Math.Max(0f, accepted);
        return accepted;
    }

    public CriticalHitProgressionState Clone()
    {
        return new CriticalHitProgressionState(
            Chance,
            DamageMultiplier,
            _maxChance,
            _maxDamageMultiplier);
    }

    private static float Clamp(float value, float minimum, float maximum)
    {
        return Math.Max(minimum, Math.Min(maximum, value));
    }
}
