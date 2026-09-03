using System.Collections.Generic;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WormSectionRollbackStateTests
    {
        [Test]
        public void BeginOrExtend_AnchorsTailAndMovesTargetBackward()
        {
            object front = new object();
            object rearA = new object();
            object rearB = new object();
            List<object> segments = new() { front, rearA, rearB };
            WormSectionRollbackState<object> state = new();

            bool shouldStart = state.BeginOrExtend(
                segments,
                splitIndex: 1,
                destroyedCount: 2,
                headDistance: 10f,
                segmentSpacing: 1f);

            Assert.That(shouldStart, Is.True);
            Assert.That(state.IsActive, Is.True);
            Assert.That(state.TargetDistance, Is.EqualTo(8f));
            Assert.That(state.AnchoredDistances[rearA], Is.EqualTo(7f));
            Assert.That(state.AnchoredDistances[rearB], Is.EqualTo(6f));
        }

        [Test]
        public void BeginOrExtend_WhileActive_ExtendsExistingTargetWithoutRestart()
        {
            object front = new object();
            object rear = new object();
            List<object> segments = new() { front, rear };
            WormSectionRollbackState<object> state = new();
            state.BeginOrExtend(segments, 1, 2, 10f, 1f);

            bool shouldStart = state.BeginOrExtend(segments, 1, 1, 9f, 1f);

            Assert.That(shouldStart, Is.False);
            Assert.That(state.TargetDistance, Is.EqualTo(7f));
        }

        [Test]
        public void AdvanceAnchoredTail_MovesTargetAndAnchorsTowardRailEnd()
        {
            object front = new object();
            object rear = new object();
            List<object> segments = new() { front, rear };
            WormSectionRollbackState<object> state = new();
            state.BeginOrExtend(segments, 1, 2, 10f, 1f);

            state.AdvanceAnchoredTail(
                segments,
                maxDistance: 20f,
                baseSpeed: 1f,
                forwardSpeedMultiplier: 2f,
                deltaTime: 0.5f);

            Assert.That(state.TargetDistance, Is.EqualTo(9f));
            Assert.That(state.AnchoredDistances[rear], Is.EqualTo(8f));
        }

        [Test]
        public void Complete_ClearsLifecycleStateAndAnchors()
        {
            object segment = new object();
            WormSectionRollbackState<object> state = new();
            state.BeginOrExtend(new[] { segment }, 0, 1, 5f, 1f);

            state.Complete();

            Assert.That(state.IsActive, Is.False);
            Assert.That(state.TargetDistance, Is.Zero);
            Assert.That(state.AnchoredDistances, Is.Empty);
        }
    }
}
