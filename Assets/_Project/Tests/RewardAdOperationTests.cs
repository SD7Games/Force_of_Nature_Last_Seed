using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class RewardAdOperationTests
    {
        [Test]
        public void TryBegin_WhenAdCompletes_ForwardsResultAndClearsPending()
        {
            DelayedRewardedAdService adService = new();
            RewardAdOperation operation = new(adService);
            bool? result = null;

            bool started = operation.TryBegin(value => result = value);
            adService.Complete(0, true);

            Assert.That(started, Is.True);
            Assert.That(result, Is.True);
            Assert.That(operation.IsPending, Is.False);
        }

        [Test]
        public void Cancel_InvalidatesLateCallback()
        {
            DelayedRewardedAdService adService = new();
            RewardAdOperation operation = new(adService);
            bool wasCalled = false;

            operation.TryBegin(_ => wasCalled = true);
            operation.Cancel();
            adService.Complete(0, true);

            Assert.That(wasCalled, Is.False);
            Assert.That(operation.IsPending, Is.False);
        }

        [Test]
        public void PreviousCallback_CannotCompleteNewOperation()
        {
            DelayedRewardedAdService adService = new();
            RewardAdOperation operation = new(adService);
            bool newOperationCompleted = false;

            operation.TryBegin(_ => { });
            operation.Cancel();
            operation.TryBegin(_ => newOperationCompleted = true);

            adService.Complete(0, true);

            Assert.That(newOperationCompleted, Is.False);
            Assert.That(operation.IsPending, Is.True);

            adService.Complete(1, true);

            Assert.That(newOperationCompleted, Is.True);
            Assert.That(operation.IsPending, Is.False);
        }

        private sealed class DelayedRewardedAdService : IRewardedAdService
        {
            private readonly List<Action<bool>> _callbacks = new();

            public bool IsReady => true;

            public void ShowRewardedAd(Action<bool> onCompleted)
            {
                _callbacks.Add(onCompleted);
            }

            public void Complete(int index, bool rewardGranted)
            {
                _callbacks[index](rewardGranted);
            }
        }
    }
}
