using System.Collections.Generic;

public static class RewardPoolInspector
{
    public static bool HasRewards(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity rarity)
    {
        return pools != null
            && pools.TryGetValue(rarity, out List<RewardModifierEntry> pool)
            && pool != null
            && pool.Count > 0;
    }

    public static RewardRarity GetHighestAvailableRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools)
    {
        if (HasRewards(pools, RewardRarity.Legendary))
            return RewardRarity.Legendary;

        if (HasRewards(pools, RewardRarity.Rare))
            return RewardRarity.Rare;

        return RewardRarity.Common;
    }

    public static int CountRewards(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools)
    {
        if (pools == null)
            return 0;

        int count = 0;

        foreach (List<RewardModifierEntry> pool in pools.Values)
        {
            if (pool != null)
                count += pool.Count;
        }

        return count;
    }
}
