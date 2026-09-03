using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WormReviveMotionCalculatorTests
    {
        [Test]
        public void CalculateCruiseSpeed_WithoutDeceleration_CoversDistanceInDuration()
        {
            WormReviveMotionCalculator calculator = new WormReviveMotionCalculator();

            float speed = calculator.CalculateCruiseSpeed(
                rollbackDistance: 10f,
                throwDuration: 2f,
                decelerationPathFraction: 0f,
                gameplaySpeed: 1f);

            Assert.That(speed, Is.EqualTo(5f));
        }

        [Test]
        public void CalculateThrowSpeed_AtTarget_ReturnsGameplaySpeed()
        {
            WormReviveMotionCalculator calculator = new WormReviveMotionCalculator();

            float speed = calculator.CalculateThrowSpeed(
                remainingDistance: 0f,
                rollbackDistance: 10f,
                cruiseSpeed: 8f,
                decelerationPathFraction: 0.2f,
                gameplaySpeed: 2f);

            Assert.That(speed, Is.EqualTo(2f));
        }

        [Test]
        public void CalculateTravelScale_StartsSquashedAndEndsAtBaseScale()
        {
            WormReviveMotionCalculator calculator = new WormReviveMotionCalculator();

            WormScale2 start = calculator.CalculateTravelScale(0f, 1.2f, 0.7f);
            WormScale2 end = calculator.CalculateTravelScale(1f, 1.2f, 0.7f);

            Assert.That(start.X, Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(start.Y, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(end.X, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(end.Y, Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
