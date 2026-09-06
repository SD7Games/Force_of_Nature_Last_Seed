using System;
using LastSeed.Core.Pooling;
using UnityEngine;

public sealed class AcaciaThornProjectilePool
{
    private ObjectPool<AcaciaThornProjectile> _pool;

    private AcaciaThornProjectile _prefab;
    private Transform _parent;
    private IScreenBounds _screenBounds;
    private bool _initialized;

    public bool IsInitialized => _initialized;

    public void Init(
        AcaciaThornProjectile prefab,
        Transform parent,
        IScreenBounds screenBounds,
        int prewarmCount)
    {
        if (_initialized)
            return;

        if (prefab == null)
            throw new ArgumentNullException(nameof(prefab));

        if (parent == null)
            throw new ArgumentNullException(nameof(parent));

        if (screenBounds == null)
            throw new ArgumentNullException(nameof(screenBounds));

        _prefab = prefab;
        _parent = parent;
        _screenBounds = screenBounds;

        _pool = new ObjectPool<AcaciaThornProjectile>(CreateNew, Deactivate);
        _pool.Prewarm(Math.Max(0, prewarmCount));

        _initialized = true;
    }

    public AcaciaThornProjectile Spawn(
        Vector3 position,
        Vector2 direction,
        int damage,
        DamageKind damageKind,
        bool isCritical,
        float speed,
        float lifeTime,
        int bounces,
        int splitCount,
        bool canSplit)
    {
        AcaciaThornProjectileSpawnRequest request = new(
            position,
            direction,
            damage,
            damageKind,
            isCritical,
            speed,
            lifeTime,
            bounces,
            splitCount,
            canSplit);

        return _pool.Rent(request, InitializeProjectile);
    }

    public void Release(AcaciaThornProjectile projectile)
    {
        _pool?.Return(projectile);
    }

    public void ReleaseAllActive()
    {
        _pool?.ReturnAll();
    }

    private AcaciaThornProjectile CreateNew()
    {
        AcaciaThornProjectile projectile = UnityEngine.Object.Instantiate(_prefab, _parent);
        projectile.Init(this, _screenBounds);
        return projectile;
    }

    private static void Deactivate(AcaciaThornProjectile projectile)
    {
        projectile.gameObject.SetActive(false);
    }

    private static void InitializeProjectile(
        AcaciaThornProjectile projectile,
        in AcaciaThornProjectileSpawnRequest request)
    {
        projectile.Activate(
            request.Position,
            request.Direction,
            request.Damage,
            request.DamageKind,
            request.IsCritical,
            request.Speed,
            request.LifeTime,
            request.Bounces,
            request.SplitCount,
            request.CanSplit);
    }
}
