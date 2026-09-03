using System;
using System.Collections.Generic;

public sealed class WormAdaptiveHpController
{
    private const int ThousandHp = 1000;
    private const int TenThousandHp = 10000;
    private const int MillionHp = 1000000;
    private const int TenMillionHp = 10000000;

    private const float FloatComparisonTolerance = 0.0001f;

    private readonly IWormHpScalingPolicy _config;
    private readonly WormSectionHpResolver _resolver;
    private readonly IWeaponPowerProvider _weaponPowerProvider;
    private readonly IWormPathProgressProvider _pathProgressProvider;
    private readonly int _levelNumber;
    private readonly int _upgradeRebalanceInterval;
    private readonly float _minimumRebalanceInterval;

    private readonly List<IWormSectionHpTarget> _sections = new();
    private float _runtimePressureMultiplier = 1f;
    private int _pendingUpgradeChanges;
    private float _lastRebalanceTime;
    private bool _hasAppliedUpgradeRebalance;
    private bool _hasRevivedThisRun;

    public WormAdaptiveHpController(
        IWormHpScalingPolicy config,
        IWeaponPowerProvider weaponPowerProvider,
        IWormPathProgressProvider pathProgressProvider,
        WormAdaptiveHpSettings settings)
    {
        _config = config;
        _resolver = new WormSectionHpResolver(config);
        _weaponPowerProvider = weaponPowerProvider ??
            throw new ArgumentNullException(nameof(weaponPowerProvider));
        _pathProgressProvider = pathProgressProvider;
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        _levelNumber = settings.LevelNumber;
        _upgradeRebalanceInterval = settings.UpgradeRebalanceInterval;
        _minimumRebalanceInterval = settings.MinimumRebalanceInterval;
    }

    public void InitializeSections(
        IReadOnlyList<IWormSectionHpTarget> sections,
        float currentTime)
    {
        if (sections == null)
            throw new ArgumentNullException(nameof(sections));

        _sections.Clear();

        for (int index = 0; index < sections.Count; index++)
        {
            IWormSectionHpTarget section = sections[index];

            if (section != null)
                _sections.Add(section);
        }

        _sections.Sort(static (left, right) => left.HpOrder.CompareTo(right.HpOrder));

        _pendingUpgradeChanges = 0;
        _lastRebalanceTime = currentTime;
        _hasAppliedUpgradeRebalance = false;
        _hasRevivedThisRun = false;

        WeaponPowerSnapshot power = GetWeaponPower();
        int previousHp = 0;

        for (int sectionIndex = 0; sectionIndex < _sections.Count; sectionIndex++)
        {
            IWormSectionHpTarget section = _sections[sectionIndex];
            section.Index = sectionIndex;
            int baseHp = WormSectionHPGenerator.GetHP(sectionIndex, _levelNumber);
            int hp = ResolveSectionHp(baseHp, sectionIndex, _sections.Count, power, 1f);
            hp = EnsureHpAbovePrevious(hp, previousHp);
            section.InitializeHp(hp);
            previousHp = hp;
        }
    }

    public void Reset(float currentTime)
    {
        _sections.Clear();
        _runtimePressureMultiplier = 1f;
        _pendingUpgradeChanges = 0;
        _lastRebalanceTime = currentTime;
        _hasAppliedUpgradeRebalance = false;
        _hasRevivedThisRun = false;
    }

    public void SetRuntimePressureMultiplier(float multiplier)
    {
        float clampedMultiplier = Math.Max(1f, multiplier);

        if (Math.Abs(_runtimePressureMultiplier - clampedMultiplier) <= FloatComparisonTolerance)
            return;

        _runtimePressureMultiplier = clampedMultiplier;

        if (UsesDynamicHp)
            RebalanceFutureSections();
    }

    public void NotifyWeaponRuntimeStatsChanged(float currentTime)
    {
        if (!UsesDynamicHp || _sections.Count == 0)
            return;

        _pendingUpgradeChanges++;

        if (!_hasAppliedUpgradeRebalance
            || _pendingUpgradeChanges >= _upgradeRebalanceInterval
            || currentTime - _lastRebalanceTime >= _minimumRebalanceInterval)
        {
            _pendingUpgradeChanges = 0;
            _lastRebalanceTime = currentTime;
            _hasAppliedUpgradeRebalance = true;
            RebalanceFutureSections();
        }
    }

    public void NotifyReviveGranted()
    {
        if (_hasRevivedThisRun)
            return;

        _hasRevivedThisRun = true;

        if (_sections.Count > 0 && _config != null && _config.Enabled)
            RebalanceFutureSections(allowHpDecrease: true);
    }

    private bool UsesDynamicHp => _config != null && _config.UsesDynamicHp;

    private void RebalanceFutureSections(bool allowHpDecrease = false)
    {
        if (_sections.Count == 0)
            return;

        WeaponPowerSnapshot power = GetWeaponPower();

        if (!power.IsValid)
            return;

        int previousHp = 0;
        float pathPressure = GetHeadPathPressureMultiplier();

        for (int index = 0; index < _sections.Count; index++)
        {
            IWormSectionHpTarget section = _sections[index];
            int sectionIndex = section != null ? section.Index : index;
            int baseHp = WormSectionHPGenerator.GetHP(sectionIndex, _levelNumber);
            int hp = ResolveSectionHp(baseHp, sectionIndex, _sections.Count, power, pathPressure);
            hp = EnsureHpAbovePrevious(hp, previousHp);

            if (CanRebalanceSection(section, allowHpDecrease))
            {
                if (!allowHpDecrease)
                    hp = Math.Max(hp, GetCurrentSectionMaxHp(section));

                section.ResetHp(hp);
                previousHp = hp;
            }
            else
            {
                previousHp = GetPreviousHpForLockedSection(
                    previousHp,
                    hp,
                    section,
                    allowHpDecrease);
            }
        }
    }

    private int ResolveSectionHp(
        int baseHp,
        int sectionIndex,
        int totalSections,
        WeaponPowerSnapshot power,
        float headPathPressureMultiplier)
    {
        return _resolver.ResolveHp(
            baseHp,
            sectionIndex,
            totalSections,
            _levelNumber,
            power,
            _runtimePressureMultiplier,
            headPathPressureMultiplier,
            _hasRevivedThisRun);
    }

    private float GetHeadPathPressureMultiplier()
    {
        return _config == null || _pathProgressProvider == null
            ? 1f
            : _config.GetHeadPathPressureMultiplier(
                _pathProgressProvider.HeadControlPointProgressNormalized);
    }

    private WeaponPowerSnapshot GetWeaponPower()
    {
        return UsesDynamicHp
            ? _weaponPowerProvider.GetCurrentPower()
            : WeaponPowerSnapshot.Invalid;
    }

    private static bool CanRebalanceSection(
        IWormSectionHpTarget section,
        bool allowHpDecrease)
    {
        return section != null
            && !section.IsDestroyed
            && !section.HasTakenDamage
            && (allowHpDecrease || !section.HasVisibleAliveSegment);
    }

    private static int GetCurrentSectionMaxHp(IWormSectionHpTarget section)
    {
        return section != null ? Math.Max(0, section.MaxHp) : 0;
    }

    private static int GetPreviousHpForLockedSection(
        int previousHp,
        int resolvedHp,
        IWormSectionHpTarget section,
        bool allowHpDecrease)
    {
        int currentMaxHp = GetCurrentSectionMaxHp(section);

        if (!allowHpDecrease)
            return Math.Max(previousHp, Math.Max(resolvedHp, currentMaxHp));

        return section != null && !section.IsDestroyed
            ? Math.Max(previousHp, Math.Max(resolvedHp, currentMaxHp))
            : Math.Max(previousHp, resolvedHp);
    }

    private static int EnsureHpAbovePrevious(int hp, int previousHp)
    {
        if (previousHp <= 0)
            return Math.Max(1, hp);

        if (previousHp >= WeaponRuntimeState.MaxProjectileDamage)
            return WeaponRuntimeState.MaxProjectileDamage;

        int minimumIncrease = GetMinimumVisibleHpIncrease(previousHp);
        return Math.Min(
            WeaponRuntimeState.MaxProjectileDamage,
            Math.Max(hp, previousHp + minimumIncrease));
    }

    private static int GetMinimumVisibleHpIncrease(int previousHp)
    {
        if (previousHp < ThousandHp) return 1;
        if (previousHp < TenThousandHp) return 100;
        if (previousHp < MillionHp) return ThousandHp;
        if (previousHp < TenMillionHp) return 100000;
        return MillionHp;
    }
}
