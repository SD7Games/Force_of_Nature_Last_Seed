using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WormSectionRollbackMotionControllerTests
    {
        [Test]
        public void Advance_MovesHeadAndAnchoredTailUsingOneExplicitStep()
        {
            object front = new();
            object tail = new();
            object[] segments = { front, tail };
            WormSectionRollbackState<object> state = new();
            state.BeginOrExtend(segments, 1, 2, 10f, 1f);
            WormSectionRollbackMotionController<object> controller = new(state);

            WormSectionRollbackMotionResult result = controller.Advance(
                10f, segments, 20f, 1f, 2f, 1f, 0.5f);

            Assert.That(result.HeadDistance, Is.EqualTo(9.5f));
            Assert.That(result.Completed, Is.False);
            Assert.That(state.TargetDistance, Is.EqualTo(9f));
            Assert.That(state.AnchoredDistances[tail], Is.EqualTo(8f));
        }

        [Test]
        public void Advance_WhenHeadReachesTarget_ReportsCompletion()
        {
            object segment = new();
            object[] segments = { segment };
            WormSectionRollbackState<object> state = new();
            state.BeginOrExtend(segments, 0, 1, 5f, 1f);
            WormSectionRollbackMotionController<object> controller = new(state);

            WormSectionRollbackMotionResult result = controller.Advance(
                5f, segments, 10f, 0f, 0f, 10f, 1f);

            Assert.That(result.HeadDistance, Is.EqualTo(4f));
            Assert.That(result.Completed, Is.True);
        }
    }
}
