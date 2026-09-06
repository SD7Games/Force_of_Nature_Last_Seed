using System;

namespace LastSeed.Core.Timing
{
    public sealed class TimedBurst
    {
        private float _timeUntilNextShot;
        private int _shotsRemaining;

        public bool IsActive => _shotsRemaining > 0;
        public bool IsShotReady { get; private set; }
        public int ShotsRemaining => _shotsRemaining;

        public void Begin(int shotCount)
        {
            _shotsRemaining = Math.Max(1, shotCount);
            _timeUntilNextShot = 0f;
            IsShotReady = true;
        }

        public void Advance(float deltaTime)
        {
            if (!IsActive || IsShotReady)
                return;

            _timeUntilNextShot -= Math.Max(0f, deltaTime);

            if (_timeUntilNextShot <= 0f)
                IsShotReady = true;
        }

        public void CommitShot(float nextShotDelay)
        {
            if (!IsActive || !IsShotReady)
                throw new InvalidOperationException("Timed burst has no ready shot to commit.");

            _shotsRemaining--;
            IsShotReady = false;

            if (!IsActive)
            {
                _timeUntilNextShot = 0f;
                return;
            }

            _timeUntilNextShot = Math.Max(0f, nextShotDelay);
        }

        public void Reset()
        {
            _timeUntilNextShot = 0f;
            _shotsRemaining = 0;
            IsShotReady = false;
        }
    }
}
