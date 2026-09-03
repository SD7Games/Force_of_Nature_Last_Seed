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

        _prefab = prefab;
        _parent = parent;
        _screenBounds = screenBounds;

        _pool = new ObjectPool<AcaciaThornProjectile>(CreateNew, Deactivate);
        _pool.Prewarm(prewarmCount);

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
        AcaciaThornProjectile projectile = _pool.Rent();

        try
        {
            projectile.Activate(
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

            return projectile;
        }
        catch
        {
            _pool.Return(projectile);
            throw;
        }
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
        AcaciaThornProjectile projectile = Object.Instantiate(_prefab, _parent);
        projectile.Init(this, _screenBounds);
        return projectile;
    }

    private static void Deactivate(AcaciaThornProjectile projectile)
    {
        projectile.gameObject.SetActive(false);
    }
}
