using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class RewardRequestQueueTests
    {
        [Test]
        public void TryDequeue_ReturnsRequestsInArrivalOrder()
        {
            RewardRequestQueue queue = new();
            RewardOpenRequest first = new(null, new RewardRollContext(0.1f, 0.2f, false));
            RewardOpenRequest second = new(null, new RewardRollContext(0.3f, 0.4f, true));
            queue.Enqueue(first);
            queue.Enqueue(second);

            Assert.That(queue.TryDequeue(out RewardOpenRequest firstResult), Is.True);
            Assert.That(queue.TryDequeue(out RewardOpenRequest secondResult), Is.True);
            Assert.That(firstResult.RollContext.HeadPathProgressNormalized, Is.EqualTo(0.1f));
            Assert.That(secondResult.RollContext.HeadPathProgressNormalized, Is.EqualTo(0.3f));
            Assert.That(queue.Count, Is.Zero);
        }

        [Test]
        public void Clear_RemovesPendingRequests()
        {
            RewardRequestQueue queue = new();
            queue.Enqueue(new RewardOpenRequest(null, default));

            queue.Clear();

            Assert.That(queue.TryDequeue(out _), Is.False);
        }
    }
}
