using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProjectileWeapon : MonoBehaviour
{
    private WeaponConfig _config;
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

    public void ApplyConfig(WeaponConfig config)
    {
        _config = config;
        _cooldown = 0f;

        _modifiers.Clear();

        foreach (var modifierData in config.Modifiers)
        {
            if (modifierData == null) continue;
            _modifiers.Add(modifierData.CreateRuntime());
        }
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
        _shots.Clear();
        _shots.Add(new ShotSpawnData(_firePoint.position, _firePoint.rotation));

        var context = new ShotContext(_firePoint, _pool, _config.Projectile);

        foreach (var modifier in _modifiers)
            modifier.Apply(_shots, context);

        foreach (var shot in _shots)
            Spawn(shot);
    }

    private void Spawn(ShotSpawnData shot)
    {
        var projectile = _pool.Get();
        projectile.ApplyConfig(_config.Projectile);
        projectile.Activate(shot.Position, shot.Rotation);
    }
}