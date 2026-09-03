namespace LastSeed.Gameplay.Signals
{
    public sealed class WormDamageDealtSignal
    {
        public WormDamageDealtSignal(DamageViewRequest request)
        {
            Request = request;
        }

        public DamageViewRequest Request { get; }
    }
}
