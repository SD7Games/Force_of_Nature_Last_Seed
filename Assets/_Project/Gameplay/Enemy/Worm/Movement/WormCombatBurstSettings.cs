using System;

public readonly struct WormCombatBurstSettings
{
    public WormCombatBurstSettings(
        bool enabled,
        float burstSpeed,
        float interval,
        float duration,
        float slowdownDuration)
    {
        Enabled = enabled;
        BurstSpeed = Math.Max(0f, burstSpeed);
        Interval = Math.Max(0.1f, interval);
        Duration = Math.Max(0.1f, duration);
        SlowdownDuration = Math.Max(0.01f, slowdownDuration);
    }

    public bool Enabled { get; }
    public float BurstSpeed { get; }
    public float Interval { get; }
    public float Duration { get; }
    public float SlowdownDuration { get; }
}
