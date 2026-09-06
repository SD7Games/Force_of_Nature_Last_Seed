using System.Collections.Generic;
using LastSeed.Core.Timing;
using LastSeed.Gameplay.Signals;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public sealed class ProjectileWeapon : MonoBehaviour, IWeapon
{
    [Header("Debug / Safety")]
    [SerializeField][Min(1)] private int _maxShots = 200;

    private WeaponConfig _config;
    private ProjectilePool _pool;
    private Transform _firePoint;

    private float _weaponCooldownTimer;
    private float _currentShotCooldown;
    private float _preparedAttackElapsed;
    private float _lastAttackReleaseDelay;
    private bool _isAttackPrepared;

    private readonly List<ShotSpawnData> _shots = new();
    private readonly ProjectileShotPatternBuilder _shotPatternBuilder = new();
    private readonly TimedBurst _salvo = new();

    private WeaponRuntimeState _runtimeState;
    private SignalBus _signalBus;

    public WeaponConfig Config => _config;
    public WeaponRuntimeState RuntimeState => _runtimeState;
    public int CurrentProjectileDamage => BuildProjectileDamage();

    [Inject]
    public void Construct(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    public void Init(ProjectilePool pool, Transform firePoint)
    {
        _pool = pool;
        _firePoint = firePoint;
    }

    public void ApplyConfig(WeaponConfig config)
    {
        _config = config;

        if (_runtimeState == null)
            _runtimeState = new WeaponRuntimeState();

        ApplyRuntimeLimits();

        RebuildModifiers(resetFiringCycle: true);
        PublishRuntimeStatsChanged();
    }

    public void Tick(float deltaTime)
    {
        if (_pool == null || _firePoint == null || _config == null) return;

        if (_salvo.IsActive)
        {
            TickSalvo(deltaTime);
            return;
        }

        if (_isAttackPrepared)
        {
            TickPreparedAttack(deltaTime);
            return;
        }

        _weaponCooldownTimer -= deltaTime;

        if (_weaponCooldownTimer <= 0f)
            StartAttackCycle();
    }

    private void RebuildModifiers(bool resetFiringCycle)
    {
        if (_config == null) return;

        float cappedFireRateBonus = Mathf.Min(
            _runtimeState.FireRateBonus,
            _config.MaxFireRateBonus);

        _currentShotCooldown = Mathf.Max(
            _config.MinShotCooldown,
            _config.FireRate / (1f + cappedFireRateBonus));

        if (resetFiringCycle)
        {
            ResetFiringCycle();
            return;
        }

        if (!_isAttackPrepared && !_salvo.IsActive)
            _weaponCooldownTimer = Mathf.Min(_weaponCooldownTimer, _currentShotCooldown);
    }

    public void ForceRebuild()
    {
        RebuildModifiers(resetFiringCycle: false);
        PublishRuntimeStatsChanged();
    }

    public void ClearTransientState()
    {
        _isAttackPrepared = false;
        _preparedAttackElapsed = 0f;
        _lastAttackReleaseDelay = 0f;
        _salvo.Reset();
    }

    public void ResetRuntimeState()
    {
        if (_runtimeState == null)
            _runtimeState = new WeaponRuntimeState();
        else
            _runtimeState.ResetProgression();

        if (_config != null)
        {
            ApplyRuntimeLimits();
            RebuildModifiers(resetFiringCycle: true);
        }
        else
        {
            ClearTransientState();
            _weaponCooldownTimer = 0f;
        }

        PublishRuntimeStatsChanged();
    }

    private void ApplyRuntimeLimits()
    {
        if (_config == null || _runtimeState == null)
            return;

        _runtimeState.SetFireRateBonusLimit(_config.MaxFireRateBonus);
        _runtimeState.SetProjectileSpeedBonusLimit(_config.MaxProjectileSpeedBonus);
        _runtimeState.SetProgressionLimits(
            _config.MaxDamageMultiplier,
            _config.MaxCriticalChance,
            _config.MaxCriticalDamageMultiplier,
            _config.MaxPenetrationBonus,
            _config.MaxParallelProjectiles,
            _config.MaxSalvoExtraShots);
    }

    private void ResetFiringCycle()
    {
        _isAttackPrepared = false;
        _preparedAttackElapsed = 0f;
        _lastAttackReleaseDelay = 0f;
        _salvo.Reset();
        _weaponCooldownTimer = 0f;
    }

    private void StartAttackCycle()
    {
        _isAttackPrepared = true;
        _preparedAttackElapsed = 0f;
        _lastAttackReleaseDelay = 0f;

        if (_signalBus == null)
        {
            ReleasePreparedAttack();
            return;
        }

        _signalBus.Fire(new WeaponAttackCycleStartedSignal(
            _currentShotCooldown,
            GetBaseShotCooldown()));
    }

    private float GetBaseShotCooldown()
    {
        if (_config == null)
            return _currentShotCooldown;

        return Mathf.Max(_config.MinShotCooldown, _config.FireRate);
    }

    public void ReleasePreparedAttack()
    {
        ReleasePreparedAttack(_preparedAttackElapsed);
    }

    public void ReleasePreparedAttack(float preparedAttackElapsed)
    {
        if (!_isAttackPrepared)
            return;

        if (_pool == null || _firePoint == null || _config == null || _runtimeState == null)
            return;

        _isAttackPrepared = false;
        _lastAttackReleaseDelay = Mathf.Clamp(preparedAttackElapsed, 0f, _currentShotCooldown);
        _preparedAttackElapsed = 0f;
        _salvo.Begin(1 + Mathf.Max(0, _runtimeState.SalvoExtraShots));

        FireSalvoShot();
    }

    private void TickPreparedAttack(float deltaTime)
    {
        _preparedAttackElapsed += deltaTime;

        if (_preparedAttackElapsed >= _currentShotCooldown)
            ReleasePreparedAttack(_currentShotCooldown);
    }

    private void TickSalvo(float deltaTime)
    {
        _salvo.Advance(deltaTime);

        if (!_salvo.IsShotReady)
            return;

        FireSalvoShot();
    }

    private void FireSalvoShot()
    {
        Fire();
        _salvo.CommitShot(GetSalvoInterval());

        if (!_salvo.IsActive)
            StartWeaponCooldown();
    }

    private void StartWeaponCooldown()
    {
        _weaponCooldownTimer = Mathf.Max(0f, _currentShotCooldown - _lastAttackReleaseDelay);
    }

    private float GetSalvoInterval()
    {
        return Mathf.Max(
            0.01f,
            _runtimeState.SalvoInterval / GetProjectileSpeedMultiplier());
    }

    private void Fire()
    {
        _shots.Clear();
        _shotPatternBuilder.Build(_firePoint.position, _firePoint.rotation, _runtimeState, _shots);

        if (_shots.Count > _maxShots)
        {
            Debug.LogWarning($"Shot limit exceeded: {_shots.Count} → clamped to {_maxShots}");
            _shots.RemoveRange(_maxShots, _shots.Count - _maxShots);
        }

        foreach (var shot in _shots)
        {
            Spawn(shot);
        }
    }

    private void Spawn(ShotSpawnData shot)
    {
        ProjectileRuntimeStats stats = BuildProjectileStats();
        _pool.Spawn(_config.Projectile, stats, shot.Position, shot.Rotation);
    }

    private ProjectileRuntimeStats BuildProjectileStats()
    {
        int finalDamage = BuildProjectileDamage();

        return new ProjectileRuntimeStats(
            finalDamage,
            _runtimeState.PenetrationBonus,
            _runtimeState.CriticalChance,
            _runtimeState.CriticalDamageMultiplier,
            GetProjectileSpeedMultiplier()
        );
    }

    private float GetProjectileSpeedMultiplier()
    {
        return Mathf.Max(0.1f, 1f + _runtimeState.ProjectileSpeedBonus);
    }

    private int BuildProjectileDamage()
    {
        if (_config == null || _config.Projectile == null || _runtimeState == null)
            return 0;

        return WeaponRuntimeState.ClampDamage(
            _config.Projectile.Damage * (double)_runtimeState.DamageMultiplier);
    }

    private void PublishRuntimeStatsChanged()
    {
        _signalBus?.Fire(new WeaponRuntimeStatsChangedSignal(
            WeaponRuntimeStatsSource.MainProjectile,
            Time.time));
    }
}
