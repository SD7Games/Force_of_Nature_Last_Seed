using System.Collections.Generic;

public static class RewardPoolBuilder
{
    private const float NewWeaponUnlockMinWormProgress = 0.3f;

    public static Dictionary<RewardRarity, List<RewardModifierEntry>> Build(
        IReadOnlyList<RewardModifierEntry> source,
        RewardRuntimeContext context,
        RewardRollContext rollContext)
    {
        var pools = new Dictionary<RewardRarity, List<RewardModifierEntry>>();

        if (source == null || context == null)
            return pools;

        for (int i = 0; i < source.Count; i++)
        {
            RewardModifierEntry entry = source[i];

            if (!CanEnterPool(entry, context, rollContext))
                continue;

            if (!pools.TryGetValue(entry.Rarity, out List<RewardModifierEntry> pool))
            {
                pool = new List<RewardModifierEntry>();
                pools.Add(entry.Rarity, pool);
            }

            pool.Add(entry);
        }

        return pools;
    }

    private static bool CanEnterPool(
        RewardModifierEntry entry,
        RewardRuntimeContext context,
        RewardRollContext rollContext)
    {
        if (entry == null || entry.Effect == null || !entry.Effect.CanApply(context))
            return false;

        return !RewardSelectionPolicy.IsNewWeaponUnlockReward(entry)
            || rollContext.WormDestructionProgressNormalized >= NewWeaponUnlockMinWormProgress;
    }
}
