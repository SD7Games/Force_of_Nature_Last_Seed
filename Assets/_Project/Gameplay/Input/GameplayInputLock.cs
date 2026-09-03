using System;

namespace LastSeed.Gameplay.Input
{
    public sealed class GameplayInputLock : IGameplayInputLock
    {
        private int _activeLockCount;

        public bool IsLocked => _activeLockCount > 0;

        public IDisposable Acquire()
        {
            _activeLockCount++;
            return new LockHandle(this);
        }

        private void Release()
        {
            if (_activeLockCount > 0)
                _activeLockCount--;
        }

        private sealed class LockHandle : IDisposable
        {
            private GameplayInputLock _owner;

            public LockHandle(GameplayInputLock owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                if (_owner == null)
                    return;

                _owner.Release();
                _owner = null;
            }
        }
    }
}
