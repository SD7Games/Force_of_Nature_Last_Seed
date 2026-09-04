using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WormForwardMotionControllerTests
    {
        [Test]
        public void Advance_BeforeCombatTarget_UsesCatchUpSpeed()
        {
            WormForwardMotionController controller = CreateController();
            var rail = new FakeRail(totalLength: 10f, pointIndex: 2, pointDistance: 5f);
            WormForwardMotionSettings settings = CreateSettings(
                catchUpPointIndex: 2,
                baseSpeed: 1f,
                catchUpSpeed: 3f);

            WormForwardMotionResult result = controller.Advance(
                headDistance: 0f,
                deltaTime: 1f,
                rail,
                settings);

            Assert.That(result.HeadDistance, Is.EqualTo(3f));
            Assert.That(result.IsCatchingUp, Is.True);
            Assert.That(result.CompletedPath, Is.False);
        }

        [Test]
        public void Advance_WhenTargetIsCrossed_ClampsAndReportsCompletion()
        {
            WormForwardMotionController controller = CreateController();
            var rail = new FakeRail(totalLength: 10f, pointIndex: 2, pointDistance: 0f);
            WormForwardMotionSettings settings = CreateSettings(
                catchUpPointIndex: 99,
                baseSpeed: 1f,
                catchUpSpeed: 3f);

            WormForwardMotionResult result = controller.Advance(
                headDistance: 9.5f,
                deltaTime: 1f,
                rail,
                settings);

            Assert.That(result.HeadDistance, Is.EqualTo(10f));
            Assert.That(result.IsCatchingUp, Is.False);
            Assert.That(result.CompletedPath, Is.True);
        }

        private static WormForwardMotionController CreateController()
        {
            return new WormForwardMotionController(
                new WormCombatBurstController(),
                new WormRailTargetResolver());
        }

        private static WormForwardMotionSettings CreateSettings(
            int catchUpPointIndex,
            float baseSpeed,
            float catchUpSpeed)
        {
            var burstSettings = new WormCombatBurstSettings(
                enabled: false,
                burstSpeed: 0f,
                interval: 1f,
                duration: 1f,
                slowdownDuration: 1f);

            return new WormForwardMotionSettings(
                baseSpeed,
                catchUpSpeed,
                catchUpPointIndex,
                catchUpStopOffset: 0f,
                catchUpExtraDistance: 0f,
                burstDisableRailPointIndex: -1,
                burstDisablePathProgress: 1f,
                burstSettings);
        }

        private sealed class FakeRail : IWormRailPath
        {
            private readonly int _pointIndex;
            private readonly float _pointDistance;

            public FakeRail(float totalLength, int pointIndex, float pointDistance)
            {
                TotalLength = totalLength;
                _pointIndex = pointIndex;
                _pointDistance = pointDistance;
            }

            public float TotalLength { get; }

            public bool TryGetControlPointDistance(int pointIndex, out float distance)
            {
                distance = _pointDistance;
                return pointIndex == _pointIndex;
            }
        }
    }
}
