using UnityEngine;

/// <summary>
/// Controls player's weapon firing logic.
///
/// The shooter owns a weapon instance that handles projectile spawning
/// and firing behaviour. Weapon configuration is defined using
/// WeaponConfig ScriptableObjects.
///
/// Shooting is tick-based and executed every frame while enabled.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerShooter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ProjectileWeapon _weapon;

    [SerializeField] private PoolRegistry _registry;
    [SerializeField] private Transform _firePoint;

    [Header("Start Weapon")]
    [SerializeField] private WeaponConfig _startConfig;

    private bool _canShoot;

    /// <summary>
    /// Initializes weapon and retrieves the required projectile pool
    /// from PoolRegistry.
    /// </summary>
    private void Awake()
    {
        var projectilePrefab = _startConfig.Projectile.Prefab;
        var pool = _registry.GetPool(projectilePrefab);

        _weapon.Init(pool, _firePoint);
        _weapon.ApplyConfig(_startConfig);
    }

    private void Start()
    {
        _canShoot = true;
    }

    /// <summary>
    /// Updates weapon firing logic every frame while shooting is enabled.
    /// </summary>
    private void Update()
    {
        if (!_canShoot) return;
        _weapon.Tick();
    }

    public void EnableShooting() => _canShoot = true;

    public void DisableShooting() => _canShoot = false;
}