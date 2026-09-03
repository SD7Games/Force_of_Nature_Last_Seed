public interface IWormHpScalingPolicy
{
    bool Enabled { get; }
    bool UsesDynamicHp { get; }
    int BaseSectionHp { get; }
    float DynamicHpWeight { get; }
    float MaxDynamicHpMultiplier { get; }
    bool UseBaseHpAsFloor { get; }
    int MinHp { get; }
    int MaxHp { get; }
    float HpMultiplier { get; }

    float GetLevelMultiplier(int levelNumber);
    float GetBaseHpMultiplier(int sectionIndex, int totalSections);
    float GetTargetSectionLifetime(int sectionIndex, int totalSections);
    float GetPressureMultiplier(int sectionIndex, int totalSections);
    float GetHeadPathPressureMultiplier(float headProgressNormalized);
    float GetPostReviveHpMultiplier(bool hasRevivedThisRun);
}
