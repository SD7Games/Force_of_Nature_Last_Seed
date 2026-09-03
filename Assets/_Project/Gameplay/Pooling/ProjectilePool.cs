using UnityEngine;

/// <summary>
/// Object pool responsible for recycling projectile instances.
///
/// The pool prewarms a number of projectiles during initialization
/// to avoid runtime instantiation during gameplay.
///
/// Projectiles automatically return themselves to the pool when
/// their lifecycle ends.
/// </summary>
[DisallowMultipleComponent]
public sealed class ProjectilePool : MonoBehaviour
{
    [SerializeField] private int _prewarmCount = 40;

    private Projectile _prefab;
    private IScreenBounds _screenBounds;
    private ObjectPool<Projectile> _pool;
    private bool _initialized;

    /// <summary>
    /// Assigns projectile prefab used by this pool and performs prewarming.
    /// Called once during pool initialization.
    /// </summary>
    public void SetPrefab(Projectile prefab, IScreenBounds screenBounds)
    {
        if (_initialized) return;

        _prefab = prefab;
        _screenBounds = screenBounds;
        _pool = new ObjectPool<Projectile>(CreateNew, Deactivate);
        _pool.Prewarm(_prewarmCount);
        _initialized = true;
    }

    /// <summary>
    /// Retrieves a projectile instance from the pool.
    /// Creates a new one if the pool is empty.
    /// </summary>
    public Projectile Spawn(
        ProjectileConfig config,
        ProjectileRuntimeStats stats,
        Vector3 position,
        Quaternion rotation)
    {
        ProjectileSpawnRequest request = new(config, stats, position, rotation);
        return _pool.Rent(request, InitializeProjectile);
    }

    /// <summary>
    /// Returns a projectile instance back to the pool.
    /// </summary>
    public void Release(Projectile projectile)
    {
        _pool?.Return(projectile);
    }

    public void ReleaseAllActive()
    {
        _pool?.ReturnAll();
    }

    private Projectile CreateNew()
    {
        var projectile = Instantiate(_prefab, transform);
        projectile.Init(this, _screenBounds);
        return projectile;
    }

    private static void Deactivate(Projectile projectile)
    {
        projectile.gameObject.SetActive(false);
    }

    private static void InitializeProjectile(
        Projectile projectile,
        in ProjectileSpawnRequest request)
    {
        projectile.ApplyConfig(request.Config, request.Stats);
        projectile.Activate(request.Position, request.Rotation);
    }
}
