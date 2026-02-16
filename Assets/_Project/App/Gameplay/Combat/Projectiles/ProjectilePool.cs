using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProjectilePool : MonoBehaviour
{
    [Header("Prefab & Prewarm")]
    [SerializeField] private Projectile _prefab;

    [SerializeField] private int _prewarmCount = 50;

    private readonly Queue<Projectile> _pool = new();

    private void Awake()
    {
        Prewarm();
    }

    public void Release(Projectile projectile)
    {
        projectile.gameObject.SetActive(false);
        _pool.Enqueue(projectile);
    }

    public Projectile Get()
    {
        if (_pool.Count == 0) return CreateNew();

        return _pool.Dequeue();
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