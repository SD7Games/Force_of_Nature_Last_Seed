using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class WormCombatBurstControllerTests
    {
        private static readonly WormCombatBurstSettings Settings = new(
            enabled: true,
            burstSpeed: 3f,
            interval: 2f,
            duration: 2f,
            slowdownDuration: 1f);

        [Test]
        public void ResolveForwardSpeed_StartsBurstOnlyAfterCombatInterval()
        {
            WormCombatBurstController controller = new WormCombatBurstController();
            controller.Reset(baseSpeed: 1f);

            float firstFrame = Tick(controller, deltaTime: 1f);
            float intervalFrame = Tick(controller, deltaTime: 1f);
            float burstFrame = Tick(controller, deltaTime: 1f);

            Assert.That(firstFrame, Is.EqualTo(1f));
            Assert.That(intervalFrame, Is.EqualTo(1f));
            Assert.That(burstFrame, Is.EqualTo(3f));
            Assert.That(controller.IsActive, Is.True);
        }

        [Test]
        public void ResolveForwardSpeed_WhenCatchingUp_StopsBurstAndUsesCatchUpSpeed()
        {
            WormCombatBurstController controller = new WormCombatBurstController();
            controller.Reset(baseSpeed: 1f);
            Tick(controller, 1f);
            Tick(controller, 1f);
            Tick(controller, 1f);

            float speed = controller.ResolveForwardSpeed(
                deltaTime: 0.1f,
                baseSpeed: 1f,
                catchUpSpeed: 5f,
                isCatchingUp: true,
                canUseBurst: true,
                Settings);

            Assert.That(speed, Is.EqualTo(5f));
            Assert.That(controller.IsActive, Is.False);
        }

        [Test]
        public void ResolveForwardSpeed_AfterBurst_DeceleratesWithoutUndershootingBaseSpeed()
        {
            WormCombatBurstController controller = new WormCombatBurstController();
            controller.Reset(baseSpeed: 1f);
            Tick(controller, 1f);
            Tick(controller, 1f);
            Tick(controller, 1f);

            float speed = Tick(controller, deltaTime: 2f);

            Assert.That(speed, Is.EqualTo(1f));
            Assert.That(controller.IsActive, Is.False);
        }

        private static float Tick(WormCombatBurstController controller, float deltaTime)
        {
            return controller.ResolveForwardSpeed(
                deltaTime,
                baseSpeed: 1f,
                catchUpSpeed: 5f,
                isCatchingUp: false,
                canUseBurst: true,
                Settings);
        }
    }
}
