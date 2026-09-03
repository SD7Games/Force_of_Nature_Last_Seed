using System;
using LastSeed.Gameplay.Input;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class GameplayInputLockTests
    {
        [Test]
        public void Acquire_WhenMultipleOwnersExist_RemainsLockedUntilEveryOwnerReleases()
        {
            GameplayInputLock gameplayInputLock = new();

            IDisposable firstLockHandle = gameplayInputLock.Acquire();
            IDisposable secondLockHandle = gameplayInputLock.Acquire();

            firstLockHandle.Dispose();

            Assert.That(gameplayInputLock.IsLocked, Is.True);

            secondLockHandle.Dispose();

            Assert.That(gameplayInputLock.IsLocked, Is.False);
        }

        [Test]
        public void Dispose_WhenCalledMoreThanOnce_ReleasesOwnerOnlyOnce()
        {
            GameplayInputLock gameplayInputLock = new();
            IDisposable firstLockHandle = gameplayInputLock.Acquire();
            IDisposable secondLockHandle = gameplayInputLock.Acquire();

            firstLockHandle.Dispose();
            firstLockHandle.Dispose();

            Assert.That(gameplayInputLock.IsLocked, Is.True);

            secondLockHandle.Dispose();

            Assert.That(gameplayInputLock.IsLocked, Is.False);
        }
    }
}
