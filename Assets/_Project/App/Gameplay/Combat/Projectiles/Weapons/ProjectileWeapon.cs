using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProjectileWeapon : MonoBehaviour
{
    private ProjectileConfig _config;
    private ProjectilePool _pool;
    private Transform _firePoint;

    private float _cooldown;

    private readonly List<IShotModifier> _modifiers = new();
    private readonly List<ShotSpawnData> _shots = new();

    public void Init(ProjectilePool pool, Transform firePoint)
    {
        _pool = pool;
        _firePoint = firePoint;
    }

    public void ApplyConfig(ProjectileConfig config)
    {
        _config = config;
        _cooldown = 0f;

        _modifiers.Clear();
        _modifiers.Add(new SpreadModifier());

    }

    public void Tick()
    {
        if (_pool == null || _firePoint == null || _config == null) return;

        _cooldown -= Time.deltaTime;

        if (_cooldown <= 0)
        {
            Fire();
            _cooldown = Mathf.Max(0.02f, _config.FireRate);
        }
    }

    private void Fire()
    {
        var context = new ShotContext(_firePoint, _pool, _config);

        _shots.Clear();
        _shots.Add(new ShotSpawnData(_firePoint.position, _firePoint.rotation));

        for (int i = 0; i < _modifiers.Count; i++)
            _modifiers[i].Apply(_shots, context);

        for (int i = 0; i < _shots.Count; i++)
            SpawnProjectile(_shots[i], _config);
    }

    private void SpawnProjectile(ShotSpawnData shot, ProjectileConfig config)
    {
        var projectile = _pool.Get();
        projectile.ApplyConfig(config);
        projectile.Activate(shot.Position, shot.Rotation);
    }
}