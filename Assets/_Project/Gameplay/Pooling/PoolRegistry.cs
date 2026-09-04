using System.Collections.Generic;
using UnityEngine;

public sealed class PoolRegistry : MonoBehaviour
{
    [SerializeField] private ProjectilePool _poolPrefab;

    private readonly Dictionary<int, ProjectilePool> _pools = new();
    private IScreenBounds _screenBounds;

    public bool IsInitialized => _screenBounds != null;

    public void Init(IScreenBounds screenBounds)
    {
        if (screenBounds == null)
        {
            Debug.LogError("PoolRegistry: screen bounds are null.", this);
            return;
        }

        _screenBounds = screenBounds;
    }

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

    public void ReleaseAllActiveProjectiles()
    {
        foreach (KeyValuePair<int, ProjectilePool> entry in _pools)
        {
            if (entry.Value != null)
                entry.Value.ReleaseAllActive();
        }
    }

    private ProjectilePool CreatePool(Projectile prefab, int key)
    {
        if (_poolPrefab == null)
        {
            Debug.LogError("PoolRegistry: pool prefab is not set.", this);
            return null;
        }

        if (_screenBounds == null)
        {
            Debug.LogError("PoolRegistry: screen bounds are not initialized.", this);
            return null;
        }

        var pool = Instantiate(_poolPrefab, transform);
        pool.name = $"Pool_{prefab.name}";

        pool.SetPrefab(prefab, _screenBounds);

        _pools.Add(key, pool);
        return pool;
    }
}
