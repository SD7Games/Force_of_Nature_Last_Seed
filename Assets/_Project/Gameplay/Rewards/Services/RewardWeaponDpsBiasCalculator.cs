using UnityEngine;

public enum RewardWeaponGroup
{
    None,
    MainWeapon,
    AcaciaThorn
}

public readonly struct RewardWeaponDpsBias
{
    public const float MinImbalanceToBias = 0.2f;

    private const float MinPreferredMultiplier = 1.05f;
    private const float MaxPreferredMultiplier = 1.65f;
    private const float MinStrongerMultiplier = 0.8f;
    private const float MaxStrongerMultiplier = 0.98f;

    public static readonly RewardWeaponDpsBias None = new(
        RewardWeaponGroup.None,
        1f,
        1f);

    private readonly RewardWeaponGroup _preferredGroup;
    private readonly float _preferredMultiplier;
    private readonly float _strongerMultiplier;

    private RewardWeaponDpsBias(
        RewardWeaponGroup preferredGroup,
        float preferredMultiplier,
        float strongerMultiplier)
    {
        _preferredGroup = preferredGroup;
        _preferredMultiplier = Mathf.Max(0f, preferredMultiplier);
        _strongerMultiplier = Mathf.Max(0f, strongerMultiplier);
    }

    public static RewardWeaponDpsBias Create(
        RewardWeaponGroup preferredGroup,
        float normalizedImbalance)
    {
        float t = Mathf.Clamp01(normalizedImbalance);

        return new RewardWeaponDpsBias(
            preferredGroup,
            Mathf.Lerp(MinPreferredMultiplier, MaxPreferredMultiplier, t),
            Mathf.Lerp(MaxStrongerMultiplier, MinStrongerMultiplier, t));
    }

    public float GetMultiplier(RewardWeaponGroup group)
    {
        if (_preferredGroup == RewardWeaponGroup.None ||
            group == RewardWeaponGroup.None)
        {
            return 1f;
        }

        return group == _preferredGroup
            ? _preferredMultiplier
            : _strongerMultiplier;
    }
}

public static class RewardWeaponDpsBiasCalculator
{
    public static RewardWeaponDpsBias Calculate(RewardRuntimeContext context)
    {
        if (!HasAdditionalWeaponUnlocked(context))
            return RewardWeaponDpsBias.None;

        WeaponPowerSnapshot mainPower = EstimateMainWeaponPower(context);
        WeaponPowerSnapshot acaciaPower = EstimateAcaciaPower(context);

        if (!mainPower.IsValid || !acaciaPower.IsValid)
            return RewardWeaponDpsBias.None;

        float mainDps = Mathf.Max(0f, mainPower.EstimatedDps);
        float acaciaDps = Mathf.Max(0f, acaciaPower.EstimatedDps);
        float strongerDps = Mathf.Max(mainDps, acaciaDps);

        if (strongerDps <= 0.01f)
            return RewardWeaponDpsBias.None;

        float imbalance = Mathf.Abs(mainDps - acaciaDps) / strongerDps;

        if (imbalance < RewardWeaponDpsBias.MinImbalanceToBias)
            return RewardWeaponDpsBias.None;

        RewardWeaponGroup preferredGroup = mainDps <= acaciaDps
            ? RewardWeaponGroup.MainWeapon
            : RewardWeaponGroup.AcaciaThorn;
        float normalizedImbalance = Mathf.InverseLerp(
            RewardWeaponDpsBias.MinImbalanceToBias,
            1f,
            imbalance);

        return RewardWeaponDpsBias.Create(preferredGroup, normalizedImbalance);
    }

    private static WeaponPowerSnapshot EstimateMainWeaponPower(
        RewardRuntimeContext context)
    {
        if (context == null)
            return WeaponPowerSnapshot.Invalid;

        return context.MainWeapon != null
            ? WeaponPowerEstimator.Estimate(context.MainWeapon)
            : WeaponPowerEstimator.Estimate(
                context.MainWeaponConfig,
                context.MainWeaponState);
    }

    private static WeaponPowerSnapshot EstimateAcaciaPower(
        RewardRuntimeContext context)
    {
        if (context == null)
            return WeaponPowerSnapshot.Invalid;

        return context.AcaciaThornWeapon != null
            ? WeaponPowerEstimator.Estimate(context.AcaciaThornWeapon)
            : WeaponPowerEstimator.Estimate(
                context.AcaciaThornConfig,
                context.AcaciaThornState);
    }

    private static bool HasAdditionalWeaponUnlocked(RewardRuntimeContext context)
    {
        AcaciaThornRuntimeState acaciaState = context?.AcaciaThornState;
        return acaciaState != null && acaciaState.IsUnlocked;
    }
}
