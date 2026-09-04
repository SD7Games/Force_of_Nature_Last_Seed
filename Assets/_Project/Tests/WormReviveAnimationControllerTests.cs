using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WormReviveAnimationControllerTests
    {
        [Test]
        public void Advance_ProgressesThroughNamedPhasesAndCompletesAtTarget()
        {
            WormReviveAnimationController controller = CreateController();
            WormReviveAnimationSettings settings = CreateSettings(gameplaySpeed: 100f);
            controller.Begin(10f, 0f, settings);

            WormReviveAnimationFrame squash = controller.Advance(0.1f);
            WormReviveAnimationFrame travel = controller.Advance(0.1f);
            WormReviveAnimationFrame landing = controller.Advance(0.1f);

            Assert.That(squash.Completed, Is.False);
            Assert.That(squash.Scale.X, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(travel.HeadDistance, Is.Zero);
            Assert.That(landing.Completed, Is.True);
            Assert.That(landing.HeadDistance, Is.Zero);
            Assert.That(landing.Scale.X, Is.EqualTo(1f));
            Assert.That(controller.IsActive, Is.False);
        }

        [Test]
        public void Cancel_StopsAnimationWithoutProducingCompletion()
        {
            WormReviveAnimationController controller = CreateController();
            controller.Begin(5f, 1f, CreateSettings(gameplaySpeed: 1f));

            controller.Cancel();
            WormReviveAnimationFrame frame = controller.Advance(1f);

            Assert.That(controller.IsActive, Is.False);
            Assert.That(frame.Completed, Is.False);
            Assert.That(frame.HeadDistance, Is.EqualTo(5f));
        }

        private static WormReviveAnimationController CreateController()
        {
            return new WormReviveAnimationController(new WormReviveMotionCalculator());
        }

        private static WormReviveAnimationSettings CreateSettings(float gameplaySpeed)
        {
            return new WormReviveAnimationSettings(
                gameplaySpeed,
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
