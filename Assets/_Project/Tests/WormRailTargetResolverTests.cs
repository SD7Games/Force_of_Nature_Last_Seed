using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WormRailTargetResolverTests
    {
        [Test]
        public void CatchUpDistance_IsCachedForSameRailAndPoint()
        {
            FakeRailPath rail = new FakeRailPath(100f, 25f);
            WormRailTargetResolver resolver = new WormRailTargetResolver();

            bool firstResolved = resolver.TryGetCatchUpDistance(rail, 2, out float first);
            bool secondResolved = resolver.TryGetCatchUpDistance(rail, 2, out float second);

            Assert.That(firstResolved, Is.True);
            Assert.That(secondResolved, Is.True);
            Assert.That(first, Is.EqualTo(25f));
            Assert.That(second, Is.EqualTo(25f));
            Assert.That(rail.LookupCount, Is.EqualTo(1));
        }

        [Test]
        public void BurstDisableDistance_WhenPointIsUnavailable_UsesClampedProgress()
        {
            FakeRailPath rail = new FakeRailPath(100f, 25f, canResolve: false);
            WormRailTargetResolver resolver = new WormRailTargetResolver();

            bool resolved = resolver.TryGetBurstDisableDistance(
                rail,
                pointIndex: 4,
                fallbackPathProgress: 1.5f,
                out float distance);

            Assert.That(resolved, Is.True);
            Assert.That(distance, Is.EqualTo(100f));
        }

        [Test]
        public void Clear_InvalidatesCachedDistance()
        {
            FakeRailPath rail = new FakeRailPath(100f, 25f);
            WormRailTargetResolver resolver = new WormRailTargetResolver();
            resolver.TryGetCatchUpDistance(rail, 2, out _);

            resolver.Clear();
            resolver.TryGetCatchUpDistance(rail, 2, out _);

            Assert.That(rail.LookupCount, Is.EqualTo(2));
        }

        private sealed class FakeRailPath : IWormRailPath
        {
            private readonly float _distance;
            private readonly bool _canResolve;

            public FakeRailPath(float totalLength, float distance, bool canResolve = true)
            {
                TotalLength = totalLength;
                _distance = distance;
                _canResolve = canResolve;
            }

            public float TotalLength { get; }
            public int LookupCount { get; private set; }

            public bool TryGetControlPointDistance(int pointIndex, out float distance)
            {
                LookupCount++;
                distance = _distance;
                return _canResolve;
            }
        }
    }
}
