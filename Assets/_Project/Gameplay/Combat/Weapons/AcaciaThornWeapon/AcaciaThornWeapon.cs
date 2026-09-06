using LastSeed.Gameplay.Signals;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public sealed class AcaciaThornWeapon : MonoBehaviour
{
    [SerializeField] private AcaciaThornWeaponConfig _config;

    private readonly AcaciaThornRuntimeState _runtimeState = new();

    private Transform _firePoint;
    private float _cooldownTimer;
    private float _currentCooldown;
    private float _salvoTimer;
    private int _salvoShotsRemaining;
    private bool _isSalvoActive;
    private bool _initialized;
    private SignalBus _signalBus;
    private AcaciaThornProjectilePool _pool;

    public AcaciaThornWeaponConfig Config => _config;
    public AcaciaThornRuntimeState RuntimeState => _runtimeState;

    [Inject]
    public void Construct(
        SignalBus signalBus,
        AcaciaThornProjectilePool pool)
    {
        _signalBus = signalBus;
        _pool = pool;
    }

    public void Init(
        Transform firePoint,
        IScreenBounds screenBounds,
        Transform projectileParent)
    {
        if (_initialized)
            return;

        if (_config == null)
        {
            Debug.LogError("AcaciaThornWeapon: config is missing.", this);
            return;
        }

        if (_config.ProjectilePrefab == null)
        {
            Debug.LogError("AcaciaThornWeapon: projectile prefab is missing.", this);
            return;
        }

        if (_pool == null)
        {
            Debug.LogError("AcaciaThornWeapon: projectile pool is missing.", this);
            return;
        }

        if (firePoint == null)
        {
            Debug.LogError("AcaciaThornWeapon: fire point is missing.", this);
            return;
        }

        _firePoint = firePoint;
        ApplyRuntimeLimits();
        _runtimeState.SetBaseDamage(_config.Damage);

        _pool.Init(
            _config.ProjectilePrefab,
            projectileParent != null ? projectileParent : transform,
            screenBounds,
            _config.PrewarmCount);

        RebuildCooldown(resetTimer: true);
        _initialized = true;
        PublishRuntimeStatsChanged();
    }

    public void Tick(float deltaTime)
    {
        if (!_initialized || !_runtimeState.IsUnlocked || !_pool.IsInitialized)
            return;

        if (_isSalvoActive)
        {
            TickSalvo(deltaTime);
            return;
        }

        _cooldownTimer -= deltaTime;

        if (_cooldownTimer > 0f)
            return;

        StartSalvo();
    }

    public void Unlock(int baseDamage)
    {
        if (!_runtimeState.CanUnlock)
            return;

        int fallbackBaseDamage = _config != null ? _config.Damage : 1;
        _runtimeState.Unlock(Mathf.Max(fallbackBaseDamage, baseDamage));
        _cooldownTimer = 0f;
        _salvoTimer = 0f;
        _salvoShotsRemaining = 0;
        _isSalvoActive = false;
        PublishRuntimeStatsChanged();
    }

    public void AddDamageMultiplier(float multiplier)
    {
        if (!_runtimeState.CanApplyDamageMultiplier(multiplier))
            return;

        _runtimeState.ApplyDamageMultiplier(multiplier);
        PublishRuntimeStatsChanged();
    }

    public void AddFireRateBonus(float bonus)
    {
        if (!_runtimeState.CanApplyFireRateBonus(bonus))
            return;

        _runtimeState.AddFireRateBonus(bonus);
        RebuildCooldown(resetTimer: false);
        PublishRuntimeStatsChanged();
    }

    public void AddSalvoShots(int extraShots)
    {
        if (!_runtimeState.CanApplySalvoShots(extraShots))
            return;

        _runtimeState.AddSalvoShots(extraShots);
        PublishRuntimeStatsChanged();
    }

    public void AddProjectileSpeedBonus(float bonus)
    {
        if (!_runtimeState.CanApplyProjectileSpeedBonus(bonus))
            return;

        _runtimeState.AddProjectileSpeedBonus(bonus);
        PublishRuntimeStatsChanged();
    }

    public void AddCriticalChance(float chanceBonus)
    {
        if (!_runtimeState.CanApplyCriticalChance(chanceBonus))
            return;

        _runtimeState.AddCriticalChance(chanceBonus);
        PublishRuntimeStatsChanged();
    }

    public void AddCriticalDamageBonus(float damageBonus)
    {
        if (!_runtimeState.CanApplyCriticalDamageBonus(damageBonus))
            return;

        _runtimeState.AddCriticalDamageBonus(damageBonus);
        PublishRuntimeStatsChanged();
    }

    public void ClearTransientState()
    {
        _pool.ReleaseAllActive();
        _isSalvoActive = false;
        _salvoTimer = 0f;
        _salvoShotsRemaining = 0;
    }

    public void ResetRuntimeState()
    {
        ClearTransientState();

        int baseDamage = _config != null ? _config.Damage : 1;
        _runtimeState.ResetProgression(baseDamage);
        ApplyRuntimeLimits();
        RebuildCooldown(resetTimer: true);
        PublishRuntimeStatsChanged();
    }

    private void ApplyRuntimeLimits()
    {
        if (_config == null)
            return;

        _runtimeState.SetProgressionLimits(
            _config.MaxDamageMultiplier,
            _config.MaxFireRateBonus,
            _config.MaxSalvoExtraShots,
            _config.MaxProjectileSpeedBonus,
            _config.MaxCriticalChance,
            _config.CriticalDamageMultiplier,
            _config.MaxCriticalDamageMultiplier);
    }

    private void StartSalvo()
    {
        _salvoShotsRemaining = 1 + Mathf.Max(0, _runtimeState.SalvoExtraShots);
        FireSalvoShot();
    }

    private void TickSalvo(float deltaTime)
    {
        _salvoTimer -= deltaTime;

        if (_salvoTimer > 0f)
            return;

        FireSalvoShot();
    }

    private void FireSalvoShot()
    {
        Fire();
        _salvoShotsRemaining--;

        if (_salvoShotsRemaining <= 0)
        {
            _isSalvoActive = false;
            _cooldownTimer = _currentCooldown;
            return;
        }

        _isSalvoActive = true;
        _salvoTimer = GetSalvoInterval();
    }

    private void Fire()
    {
        Vector2 direction = _firePoint.rotation * Vector2.up;

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.up;

        direction.Normalize();

        Vector3 position = _firePoint.position +
            (Vector3)(direction * Mathf.Max(0f, _config.SpawnOffset));

        int damage = BuildDamage(out DamageKind damageKind, out bool isCritical);
        _pool.Spawn(
            position,
            direction,
            damage,
            damageKind,
            isCritical,
            GetProjectileSpeed(),
            _config.LifeTime,
            _config.BounceCount,
            GetSplitCount(),
            true);
    }

    private int BuildDamage(out DamageKind damageKind, out bool isCritical)
    {
        double rawDamage = Mathf.Max(1, _runtimeState.BaseDamage) *
            (double)_runtimeState.DamageMultiplier;

        isCritical = _runtimeState.CriticalChance > 0f &&
            UnityEngine.Random.value < _runtimeState.CriticalChance;
        damageKind = isCritical ? DamageKind.Critical : DamageKind.Normal;

        if (isCritical)
            rawDamage *= _runtimeState.CriticalDamageMultiplier;

        return AcaciaThornRuntimeState.ClampDamage(rawDamage);
    }

    private int GetSplitCount()
    {
        return Mathf.Max(0, _config.BaseSplitCount);
    }

    private float GetProjectileSpeed()
    {
        return Mathf.Max(
            0.1f,
            _config.Speed * GetProjectileSpeedMultiplier());
    }

    private float GetSalvoInterval()
    {
        return Mathf.Max(
            0.01f,
            _config.SalvoInterval / GetProjectileSpeedMultiplier());
    }

    private float GetProjectileSpeedMultiplier()
    {
        return Mathf.Max(0.1f, 1f + _runtimeState.ProjectileSpeedBonus);
    }

    private void RebuildCooldown(bool resetTimer)
    {
        float cappedFireRateBonus = Mathf.Min(
            _runtimeState.FireRateBonus,
            _config.MaxFireRateBonus);

        _currentCooldown = Mathf.Max(
            _config.MinCooldown,
            _config.Cooldown / (1f + cappedFireRateBonus));

        if (resetTimer)
            _cooldownTimer = 0f;
        else
            _cooldownTimer = Mathf.Min(_cooldownTimer, _currentCooldown);
    }

    private void PublishRuntimeStatsChanged()
    {
        _signalBus?.Fire(new WeaponRuntimeStatsChangedSignal(
            WeaponRuntimeStatsSource.AcaciaThorn,
            Time.time));
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Unlock Acacia Thorn")]
    private void DebugUnlockAcaciaThorn()
    {
        Unlock(_config != null ? _config.Damage : 1);
    }

    [ContextMenu("Debug/Fire Acacia Thorn Once")]
    private void DebugFireAcaciaThornOnce()
    {
        if (!_initialized)
        {
            Debug.LogWarning("AcaciaThornWeapon debug fire skipped: weapon is not initialized.", this);
            return;
        }

        if (!_runtimeState.IsUnlocked)
            Unlock(_config != null ? _config.Damage : 1);

        Fire();
        _cooldownTimer = _currentCooldown;
    }
#endif
}
