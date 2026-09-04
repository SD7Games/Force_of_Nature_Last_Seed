using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponShotPatternState
{
    private const int DefaultMaxParallelProjectiles = 5;
    private const int DefaultMaxSalvoExtraShots = 3;

    public const int MaxParallelProjectiles = 8;
    public const int MaxSalvoExtraShots = 5;

    private readonly List<ShotModifierData> _modifiers = new();
    private int _maxParallelProjectiles = DefaultMaxParallelProjectiles;
    private SalvoProgressionState _salvo = new(DefaultMaxSalvoExtraShots, MaxSalvoExtraShots);

    public int ParallelProjectileCount { get; private set; } = 1;
    public float ParallelSpacing { get; private set; } = 0.5f;
    public int SalvoExtraShots => _salvo.ExtraShots;
    public float SalvoInterval { get; private set; } = 0.2f;
    public IReadOnlyList<ShotModifierData> Modifiers => _modifiers;

    public bool CanAddParallelProjectiles => ParallelProjectileCount < _maxParallelProjectiles;
    public bool CanAddSalvoShots => _salvo.CanAdd;

    public void Reset()
    {
        _modifiers.Clear();
        ParallelProjectileCount = 1;
        ParallelSpacing = 0.5f;
        _salvo.Reset();
        SalvoInterval = 0.2f;
    }

    public void SetLimits(int maxParallelProjectiles, int maxSalvoExtraShots)
    {
        _maxParallelProjectiles = Mathf.Clamp(maxParallelProjectiles, 1, MaxParallelProjectiles);
        _salvo.SetLimit(maxSalvoExtraShots);
        ParallelProjectileCount = Mathf.Min(ParallelProjectileCount, _maxParallelProjectiles);
    }

    public bool CanApplyParallelProjectiles(int bonus, int limitAfterApply)
    {
        if (bonus <= 0)
            return false;

        int limit = Mathf.Clamp(
            Mathf.Max(_maxParallelProjectiles, limitAfterApply),
            1,
            MaxParallelProjectiles);
        return ParallelProjectileCount + bonus <= limit;
    }

    public bool CanApplyParallelProjectiles(int bonus)
    {
        return bonus > 0 && ParallelProjectileCount + bonus <= _maxParallelProjectiles;
    }

    public bool CanApplySalvoShots(int extraShots, int limitAfterApply)
    {
        if (extraShots <= 0)
            return false;

        return _salvo.CanApply(extraShots, limitAfterApply);
    }

    public bool CanApplySalvoShots(int extraShots)
    {
        return _salvo.CanApply(extraShots);
    }

    public int AddParallelProjectiles(int bonus, float spacing)
    {
        int accepted = Mathf.Min(Mathf.Max(0, bonus), _maxParallelProjectiles - ParallelProjectileCount);
        ParallelProjectileCount += Mathf.Max(0, accepted);

        if (accepted > 0)
            ParallelSpacing = Mathf.Max(0.1f, spacing);

        return accepted;
    }

    public int AddSalvoShots(int extraShots, float interval)
    {
        int accepted = _salvo.Add(extraShots);

        if (accepted > 0)
            SalvoInterval = Mathf.Max(0.01f, interval);

        return accepted;
    }

    public void ExpandParallelLimit(int limit)
    {
        _maxParallelProjectiles = Mathf.Clamp(
            Mathf.Max(_maxParallelProjectiles, limit),
            1,
            MaxParallelProjectiles);
    }

    public void ExpandSalvoLimit(int limit)
    {
        _salvo.ExpandLimit(limit);
    }

    public bool AddModifier(ShotModifierData modifier)
    {
        if (modifier == null)
            return false;

        if (modifier is ParallelModifierData parallel)
            return AddParallelProjectiles(Mathf.Max(0, parallel.Count - 1), parallel.Spacing) > 0;

        _modifiers.Add(modifier);
        return true;
    }

    public bool CanAddModifier(ShotModifierData modifier)
    {
        if (modifier == null)
            return false;

        return modifier is not ParallelModifierData parallel
            || CanApplyParallelProjectiles(Mathf.Max(0, parallel.Count - 1));
    }

    public WeaponShotPatternState Clone()
    {
        WeaponShotPatternState clone = new()
        {
            _maxParallelProjectiles = _maxParallelProjectiles,
            _salvo = _salvo.Clone(),
            ParallelProjectileCount = ParallelProjectileCount,
            ParallelSpacing = ParallelSpacing,
            SalvoInterval = SalvoInterval
        };

        for (int i = 0; i < _modifiers.Count; i++)
            clone._modifiers.Add(_modifiers[i]);

        return clone;
    }
}
