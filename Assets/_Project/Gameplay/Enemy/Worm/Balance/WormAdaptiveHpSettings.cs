using System;

public sealed class WormAdaptiveHpSettings
{
    public WormAdaptiveHpSettings(
        int levelNumber,
        int upgradeRebalanceInterval,
        float minimumRebalanceInterval)
    {
        LevelNumber = Math.Max(1, levelNumber);
        UpgradeRebalanceInterval = Math.Max(1, upgradeRebalanceInterval);
        MinimumRebalanceInterval = Math.Max(0f, minimumRebalanceInterval);
    }

    public int LevelNumber { get; }
    public int UpgradeRebalanceInterval { get; }
    public float MinimumRebalanceInterval { get; }
}
