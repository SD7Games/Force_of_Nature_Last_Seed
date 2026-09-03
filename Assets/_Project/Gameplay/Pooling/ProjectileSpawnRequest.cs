using UnityEngine;

public readonly struct ProjectileSpawnRequest
{
    public ProjectileSpawnRequest(
        ProjectileConfig config,
        ProjectileRuntimeStats stats,
        Vector3 position,
        Quaternion rotation)
    {
        Config = config;
        Stats = stats;
        Position = position;
        Rotation = rotation;
    }

    public ProjectileConfig Config { get; }
    public ProjectileRuntimeStats Stats { get; }
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
}
