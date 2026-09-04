using System;

public sealed class CappedBonusState
{
    private const float ComparisonEpsilon = 0.0001f;

    public CappedBonusState(float limit)
    {
        SetLimit(limit);
    }

    private CappedBonusState(float value, float limit)
    {
        Value = value;
        Limit = limit;
    }

    public float Value { get; private set; }
    public float Limit { get; private set; }
    public bool CanAdd => Value < Limit;

    public void Reset()
    {
        Value = 0f;
    }

    public void SetLimit(float limit)
    {
        Limit = Math.Max(0f, limit);
        Value = Math.Min(Value, Limit);
    }

    public bool CanApply(float bonus)
    {
        return bonus > 0f && Value + bonus <= Limit + ComparisonEpsilon;
    }

    public float Add(float bonus)
    {
        float accepted = Math.Min(Math.Max(0f, bonus), Limit - Value);
        Value += Math.Max(0f, accepted);
        return accepted;
    }

    public CappedBonusState Clone()
    {
        return new CappedBonusState(Value, Limit);
    }
}
