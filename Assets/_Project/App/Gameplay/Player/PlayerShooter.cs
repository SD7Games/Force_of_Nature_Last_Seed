using UnityEngine;

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

    private void Update()
    {
        if (!_canShoot) return;
        _weapon.Tick();
    }

    public void EnableShooting() => _canShoot = true;

    public void DisableShooting() => _canShoot = false;
}