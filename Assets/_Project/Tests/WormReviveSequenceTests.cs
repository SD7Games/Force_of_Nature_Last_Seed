using System;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WormReviveSequenceTests
    {
        [Test]
        public void CompleteAfterFinalRender_RendersBeforeNotifyingCompletion()
        {
            WormReviveSequence sequence = CreateSequence();
            string callOrder = string.Empty;

            sequence.Begin(
                1f,
                0f,
                CreateSettings(),
                Array.Empty<WormSegment>(),
                () => callOrder += "complete");

            sequence.Advance(0.1f);
            sequence.Advance(0.1f);
            WormReviveAnimationFrame finalFrame = sequence.Advance(0.1f);
            sequence.CompleteAfterFinalRender(() => callOrder += "render>");

            Assert.That(finalFrame.Completed, Is.True);
            Assert.That(callOrder, Is.EqualTo("render>complete"));
            Assert.That(sequence.IsActive, Is.False);
            Assert.That(sequence.VisualYOffset, Is.Zero);
        }

        [Test]
        public void Cancel_DiscardsPendingCompletion()
        {
            WormReviveSequence sequence = CreateSequence();
            bool completed = false;

            sequence.Begin(
                1f,
                0f,
                CreateSettings(),
                Array.Empty<WormSegment>(),
                () => completed = true);

            sequence.Cancel();
            sequence.CompleteAfterFinalRender(null);

            Assert.That(completed, Is.False);
            Assert.That(sequence.IsActive, Is.False);
        }

        private static WormReviveSequence CreateSequence()
        {
            WormReviveAnimationController animationController = new(
                new WormReviveMotionCalculator());
            return new WormReviveSequence(
                animationController,
                new WormReviveVisualScaler());
        }

        private static WormReviveAnimationSettings CreateSettings()
        {
            return new WormReviveAnimationSettings(
                gameplaySpeed: 100f,
                squashDuration: 0.1f,
                throwDuration: 0.1f,
                landingDuration: 0.1f,
                decelerationPathFraction: 0f,
                arcHeight: 1f,
                squashXScale: 1.2f,
                squashYScale: 0.8f,
                landingXScale: 1.1f,
                landingYScale: 0.9f);
        }
    }
}
