using System;

public sealed class WormSectionHpResolver
{
    private readonly IWormHpScalingPolicy _config;

    public WormSectionHpResolver(IWormHpScalingPolicy config)
    {
        _config = config;
    }

    public int ResolveHp(
        int baseHp,
        int sectionIndex,
        int totalSections,
        int levelNumber,
        WeaponPowerSnapshot power,
        float runtimePressureMultiplier,
        float headPathPressureMultiplier,
        bool hasRevivedThisRun = false)
    {
        if (_config == null || !_config.Enabled)
            return baseHp;

        float independentHp = ResolveIndependentHp(
            baseHp,
            sectionIndex,
            totalSections,
            levelNumber);

        float postReviveHpMultiplier = _config.GetPostReviveHpMultiplier(hasRevivedThisRun);

        if (!_config.UsesDynamicHp || !power.IsValid)
            return ClampHp(independentHp * postReviveHpMultiplier);

        float dynamicHp =
            power.EstimatedDps *
            _config.GetTargetSectionLifetime(sectionIndex, totalSections) *
            _config.GetLevelMultiplier(levelNumber) *
            _config.GetPressureMultiplier(sectionIndex, totalSections) *
            Math.Max(1f, runtimePressureMultiplier) *
            Math.Max(0.1f, headPathPressureMultiplier);

        if (_config.UseBaseHpAsFloor)
            dynamicHp = Math.Max(independentHp, dynamicHp);

        dynamicHp = ClampDynamicHp(independentHp, dynamicHp);

        float blendedHp = independentHp +
            (dynamicHp - independentHp) * Clamp01(_config.DynamicHpWeight);

        return ClampHp(blendedHp * _config.HpMultiplier * postReviveHpMultiplier);
    }

    private float ResolveIndependentHp(
        int baseHp,
        int sectionIndex,
        int totalSections,
        int levelNumber)
    {
        float configuredBaseHp = Math.Max(baseHp, _config.BaseSectionHp);

        return configuredBaseHp *
            _config.GetLevelMultiplier(levelNumber) *
            _config.GetBaseHpMultiplier(sectionIndex, totalSections) *
            _config.GetPressureMultiplier(sectionIndex, totalSections);
    }

    private float ClampDynamicHp(float independentHp, float dynamicHp)
    {
        float safeIndependentHp = Math.Max(1f, independentHp);
        float maxDynamicHp = safeIndependentHp * _config.MaxDynamicHpMultiplier;

        return Math.Min(dynamicHp, maxDynamicHp);
    }

    private int ClampHp(float hp)
    {
        int roundedHp = (int)Math.Round(hp, MidpointRounding.ToEven);
        return Math.Max(_config.MinHp, Math.Min(_config.MaxHp, roundedHp));
    }

    private static float Clamp01(float value)
    {
        return Math.Max(0f, Math.Min(1f, value));
    }
}
