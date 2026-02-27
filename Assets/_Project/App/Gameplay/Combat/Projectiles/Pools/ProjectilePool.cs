using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProjectilePool : MonoBehaviour
{
    [SerializeField] private int _prewarmCount = 40;

    private Projectile _prefab;
    private readonly Queue<Projectile> _pool = new();
    private bool _initialized;

    public void SetPrefab(Projectile prefab)
    {
        if (_initialized) return;

        _prefab = prefab;
        Prewarm();
        _initialized = true;
    }

    public Projectile Get()
    {
        if (_pool.Count == 0)
            return CreateNew();

        return _pool.Dequeue();
    }

    public void Release(Projectile projectile)
    {
        projectile.gameObject.SetActive(false);
        _pool.Enqueue(projectile);
    }

    private void Prewarm()
    {
        for (int i = 0; i < _prewarmCount; i++)
        {
            var projectile = CreateNew();
            Release(projectile);
        }
    }

    private Projectile CreateNew()
    {
        var projectile = Instantiate(_prefab, transform);
        projectile.Init(this);
        projectile.gameObject.SetActive(false);
        return projectile;
    }
}