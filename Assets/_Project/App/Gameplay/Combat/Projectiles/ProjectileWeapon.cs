using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProjectileWeapon : MonoBehaviour
{
    private ProjectileConfig _config;
    private ProjectilePool _pool;
    private Transform _firePoint;

    private float _cooldown;

    public void Init(ProjectilePool pool, Transform firePoint)
    {
        _pool = pool;
        _firePoint = firePoint;
    }

    public void ApplyConfig(ProjectileConfig config)
    {
        _config = config;
        _cooldown = 0f;
    }

    public void Tick()
    {
        if (_config == null) return;
        if (_pool == null || _firePoint == null) return;

        _cooldown -= Time.deltaTime;

        if (_cooldown <= 0)
        {
            Fire();
            _cooldown = Mathf.Max(0.02f, _config.FireRate);
        }
    }

    private void Fire()
    {
        var projectile = _pool.Get();
        projectile.ApplyConfig(_config);
        projectile.Activate(_firePoint.position, _firePoint.rotation);
    }
}