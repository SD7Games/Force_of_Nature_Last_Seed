using LastSeed.Core.Collections;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class UniqueReferenceQueueTests
    {
        [Test]
        public void Enqueue_DeduplicatesByReferenceAndPreservesOrder()
        {
            object first = new();
            object second = new();
            UniqueReferenceQueue<object> queue = new();

            Assert.That(queue.Enqueue(first), Is.True);
            Assert.That(queue.Enqueue(second), Is.True);
            Assert.That(queue.Enqueue(first), Is.False);

            Assert.That(queue.TryDequeue(out object firstResult), Is.True);
            Assert.That(queue.TryDequeue(out object secondResult), Is.True);
            Assert.That(firstResult, Is.SameAs(first));
            Assert.That(secondResult, Is.SameAs(second));
        }

        [Test]
        public void Remove_DeletesQueuedItemWithoutChangingOtherOrder()
        {
            object first = new();
            object removed = new();
            object last = new();
            UniqueReferenceQueue<object> queue = new();
            queue.Enqueue(first);
            queue.Enqueue(removed);
            queue.Enqueue(last);

            bool wasRemoved = queue.Remove(removed);

            Assert.That(wasRemoved, Is.True);
            Assert.That(queue.Contains(removed), Is.False);
            Assert.That(queue.TryDequeue(out object firstResult), Is.True);
            Assert.That(queue.TryDequeue(out object lastResult), Is.True);
            Assert.That(firstResult, Is.SameAs(first));
            Assert.That(lastResult, Is.SameAs(last));
        }

        [Test]
        public void Clear_RemovesQueueAndMembershipState()
        {
            object item = new();
            UniqueReferenceQueue<object> queue = new();
            queue.Enqueue(item);

            queue.Clear();

            Assert.That(queue.Count, Is.Zero);
            Assert.That(queue.Contains(item), Is.False);
            Assert.That(queue.TryDequeue(out _), Is.False);
        }
    }
}
