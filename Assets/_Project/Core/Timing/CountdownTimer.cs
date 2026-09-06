using System;

namespace LastSeed.Core.Timing
{
    public sealed class CountdownTimer
    {
        public float Remaining { get; private set; }
        public bool IsElapsed => Remaining <= 0f;

        public void Start(float duration)
        {
            Remaining = Math.Max(0f, duration);
        }

        public void Advance(float deltaTime)
        {
            if (IsElapsed)
                return;

            Remaining = Math.Max(0f, Remaining - Math.Max(0f, deltaTime));
        }

        public void LimitTo(float maximumRemaining)
        {
            Remaining = Math.Min(Remaining, Math.Max(0f, maximumRemaining));
        }

        public void Reset()
        {
            Remaining = 0f;
        }
    }
}
