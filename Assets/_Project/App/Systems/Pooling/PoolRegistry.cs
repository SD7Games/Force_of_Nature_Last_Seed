using System.Collections.Generic;
using UnityEngine;

public sealed class PoolRegistry : MonoBehaviour
{
    [SerializeField] private ProjectilePool _poolPrefab;

    private readonly Dictionary<int, ProjectilePool> _pools = new();

    public ProjectilePool GetPool(Projectile projectilePrefab)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("PoolRegistry: projectilePrefab is NULL");
            return null;
        }

        int key = projectilePrefab.GetInstanceID();

        if (_pools.TryGetValue(key, out var pool))
            return pool;

        return CreatePool(projectilePrefab, key);
    }

    private ProjectilePool CreatePool(Projectile prefab, int key)
    {
        var pool = Instantiate(_poolPrefab, transform);
        pool.name = $"Pool_{prefab.name}";

        pool.SetPrefab(prefab);

        _pools.Add(key, pool);
        return pool;
    }
}