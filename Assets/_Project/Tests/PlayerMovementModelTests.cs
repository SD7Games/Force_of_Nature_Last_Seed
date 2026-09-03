using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class PlayerMovementModelTests
    {
        [Test]
        public void Move_ClampsPositionToPlayableBounds()
        {
            PlayerMovementModel model = CreateModel(startX: 0f);

            model.Move(inputX: 1f, deltaTime: 10f);

            Assert.That(model.PositionX, Is.EqualTo(4.5f));
            Assert.That(model.MovementInput, Is.EqualTo(1f));
        }

        [Test]
        public void TouchMovement_UsesPlayableWidthAndResetRestoresStart()
        {
            PlayerMovementModel model = CreateModel(startX: 1f);

            model.MoveByNormalizedScreenDeltaX(0.5f);
            Assert.That(model.PositionX, Is.EqualTo(4.5f));

            model.Reset();

            Assert.That(model.PositionX, Is.EqualTo(1f));
            Assert.That(model.MovementInput, Is.Zero);
        }

        private static PlayerMovementModel CreateModel(float startX)
        {
            return new PlayerMovementModel(
                startX,
                speed: 8f,
                smooth: 100f,
                edgePadding: 0.5f,
                new StubScreenBounds(-5f, 5f));
        }

        private sealed class StubScreenBounds : IScreenBounds
        {
            public StubScreenBounds(float left, float right)
            {
                Left = left;
                Right = right;
            }

            public float Left { get; }
            public float Right { get; }
            public float Top => 10f;
            public float Bottom => -10f;
        }
    }
}
