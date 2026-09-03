namespace LastSeed.Infrastructure.Input
{
    public readonly struct PlayerInputSnapshot
    {
        public PlayerInputSnapshot(
            float horizontalMovement,
            bool isTouchActive,
            float normalizedTouchDeltaX)
        {
            HorizontalMovement = horizontalMovement;
            IsTouchActive = isTouchActive;
            NormalizedTouchDeltaX = normalizedTouchDeltaX;
        }

        public float HorizontalMovement { get; }

        public bool IsTouchActive { get; }

        public float NormalizedTouchDeltaX { get; }
    }
}
