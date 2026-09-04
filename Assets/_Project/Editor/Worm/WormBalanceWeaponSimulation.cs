using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

internal static class WormBalanceWeaponSimulation
{
    public static WeaponRuntimeState CreateMainWeaponState(WeaponConfig config)
    {
        WeaponRuntimeState state = new();

        if (config == null)
            return state;

        state.SetFireRateBonusLimit(config.MaxFireRateBonus);
        state.SetProjectileSpeedBonusLimit(config.MaxProjectileSpeedBonus);
        state.SetProgressionLimits(
            config.MaxDamageMultiplier,
            config.MaxCriticalChance,
            config.MaxCriticalDamageMultiplier,
            config.MaxPenetrationBonus,
            config.MaxParallelProjectiles,
            config.MaxSalvoExtraShots);

        return state;
    }

    public static AcaciaThornRuntimeState CreateAcaciaThornState(
        AcaciaThornWeaponConfig config)
    {
        AcaciaThornRuntimeState state = new();

        if (config == null)
            return state;

        state.SetProgressionLimits(
            config.MaxDamageMultiplier,
            config.MaxFireRateBonus,
            config.MaxSalvoExtraShots,
            config.MaxProjectileSpeedBonus,
            config.MaxCriticalChance,
            config.CriticalDamageMultiplier,
            config.MaxCriticalDamageMultiplier);
        state.SetBaseDamage(config.Damage);

        return state;
    }

    public static WeaponPowerSnapshot EstimatePower(
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState)
    {
        return WeaponPowerEstimator.Estimate(
            settings.MainWeaponConfig,
            mainState,
            settings.AcaciaThornConfig,
            acaciaState);
    }

    public static int BuildMainWeaponDamage(
        WeaponConfig config,
        WeaponRuntimeState state)
    {
        if (config == null || config.Projectile == null || state == null)
            return 0;

        return WeaponRuntimeState.ClampDamage(
            config.Projectile.Damage * (double)state.DamageMultiplier);
    }

}
