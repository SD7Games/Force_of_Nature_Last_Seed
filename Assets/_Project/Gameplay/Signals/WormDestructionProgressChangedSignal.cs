namespace LastSeed.Gameplay.Signals
{
    public sealed class WormDestructionProgressChangedSignal
    {
        public WormDestructionProgressChangedSignal(
            int destroyedSegments,
            int totalSegments,
            float normalizedProgress)
        {
            DestroyedSegments = destroyedSegments;
            TotalSegments = totalSegments;
            NormalizedProgress = normalizedProgress;
        }

        public int DestroyedSegments { get; }
        public int TotalSegments { get; }
        public float NormalizedProgress { get; }
    }
}
