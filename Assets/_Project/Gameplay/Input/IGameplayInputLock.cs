using System;

namespace LastSeed.Gameplay.Input
{
    public interface IGameplayInputLock
    {
        bool IsLocked { get; }

        IDisposable Acquire();
    }
}
