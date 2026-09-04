using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WormSegmentChainTests
    {
        [Test]
        public void ReplaceWith_CopiesSourceAndDoesNotExposeMutation()
        {
            object first = new();
            object second = new();
            object[] source = { first, second };
            WormSegmentChain<object> chain = new();

            chain.ReplaceWith(source);
            source[0] = new object();

            Assert.That(chain.Count, Is.EqualTo(2));
            Assert.That(chain.Segments[0], Is.SameAs(first));
        }

        [Test]
        public void RemoveAll_ReturnsCountAndOriginalFirstIndex()
        {
            object first = new();
            object removedA = new();
            object middle = new();
            object removedB = new();
            WormSegmentChain<object> chain = new();
            chain.ReplaceWith(new[] { first, removedA, middle, removedB });

            int removedCount = chain.RemoveAll(
                new[] { removedB, removedA, removedA },
                out int firstRemovedIndex);

            Assert.That(removedCount, Is.EqualTo(2));
            Assert.That(firstRemovedIndex, Is.EqualTo(1));
            Assert.That(chain.Segments, Is.EqualTo(new[] { first, middle }));
        }

        [Test]
        public void Clear_RemovesSegmentsAndStaleRemovalState()
        {
            object removed = new();
            WormSegmentChain<object> chain = new();
            chain.ReplaceWith(new[] { removed });
            chain.RemoveAll(new[] { removed }, out _);

            chain.Clear();
            chain.ReplaceWith(new[] { removed });

            Assert.That(chain.Count, Is.EqualTo(1));
        }
    }
}
