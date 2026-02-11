using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerShooter : MonoBehaviour
{
    [SerializeField] private ProjectilePool _pool;
    [SerializeField] private float _fireRate = 0.4f;

    private float _timer;

    private void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            Fire();
            _timer = _fireRate;
        }
    }

    public void SetFireRate(float newFireRate)
    {
        _fireRate = Mathf.Max(0.02f, newFireRate);
    }

    private void Fire()
    {
        var projectile = _pool.Get();
        projectile.Activate(transform.position, Quaternion.identity);
    }
}