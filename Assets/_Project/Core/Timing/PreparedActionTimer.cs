using System;

namespace LastSeed.Core.Timing
{
    public sealed class PreparedActionTimer
    {
        public bool IsActive { get; private set; }
        public float Elapsed { get; private set; }
        public float LastCompletionDelay { get; private set; }

        public void Begin()
        {
            IsActive = true;
            Elapsed = 0f;
            LastCompletionDelay = 0f;
        }

        public void Advance(float deltaTime)
        {
            if (!IsActive)
                return;

            Elapsed += Math.Max(0f, deltaTime);
        }

        public bool HasReached(float duration)
        {
            return IsActive && Elapsed >= Math.Max(0f, duration);
        }

        public bool TryComplete(float elapsed, float maximumDelay)
        {
            if (!IsActive)
                return false;

            LastCompletionDelay = Math.Min(
                Math.Max(0f, elapsed),
                Math.Max(0f, maximumDelay));
            IsActive = false;
            Elapsed = 0f;
            return true;
        }

        public void Reset()
        {
            IsActive = false;
            Elapsed = 0f;
            LastCompletionDelay = 0f;
        }
    }
}
