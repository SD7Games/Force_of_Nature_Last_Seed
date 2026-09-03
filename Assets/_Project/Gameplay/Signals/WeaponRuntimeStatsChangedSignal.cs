namespace LastSeed.Gameplay.Signals
{
    public enum WeaponRuntimeStatsSource
    {
        MainProjectile,
        AcaciaThorn
    }

    public sealed class WeaponRuntimeStatsChangedSignal
    {
        public WeaponRuntimeStatsChangedSignal(
            WeaponRuntimeStatsSource source,
            float occurredAt)
        {
            Source = source;
            OccurredAt = occurredAt;
        }

        public WeaponRuntimeStatsSource Source { get; }
        public float OccurredAt { get; }
    }
}
