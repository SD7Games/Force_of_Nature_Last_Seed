using System;

public sealed class DamageMultiplierProgressionState
{
    private const float ComparisonEpsilon = 0.0001f;
    private float _limit;

    public DamageMultiplierProgressionState(float limit)
    {
        _limit = Math.Max(1f, limit);
        Value = 1f;
    }

    private DamageMultiplierProgressionState(float value, float limit)
    {
        Value = value;
        _limit = limit;
    }

    public float Value { get; private set; }
    public bool CanAdd => Value < _limit;

    public void Reset() => Value = 1f;

    public void SetLimit(float limit)
    {
        _limit = Math.Max(1f, limit);
        Value = Math.Min(Value, _limit);
    }

    public bool CanApply(float multiplier)
    {
        return multiplier > 1f && Value * multiplier <= _limit + ComparisonEpsilon;
    }

    public float Apply(float multiplier)
    {
        if (multiplier <= 1f)
            return 0f;

        float previous = Value;
        Value = Math.Min(Value * multiplier, _limit);
        return Value - previous;
    }

    public DamageMultiplierProgressionState Clone()
    {
        return new DamageMultiplierProgressionState(Value, _limit);
    }
}
