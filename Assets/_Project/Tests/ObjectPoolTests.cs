using System;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class ObjectPoolTests
    {
        [Test]
        public void RentAndReturn_ReusesItemAndRejectsDuplicateReturn()
        {
            int createdCount = 0;
            ObjectPool<TestItem> pool = CreatePool(() => createdCount++);
            pool.Prewarm(1);

            TestItem firstRent = pool.Rent();

            Assert.That(pool.Return(firstRent), Is.True);
            Assert.That(pool.Return(firstRent), Is.False);
            Assert.That(pool.Rent(), Is.SameAs(firstRent));
            Assert.That(createdCount, Is.EqualTo(1));
        }

        [Test]
        public void Rent_WhenInitializationFails_RollsItemBack()
        {
            ObjectPool<TestItem> pool = CreatePool();

            Assert.Throws<InvalidOperationException>(() =>
                pool.Rent(_ => throw new InvalidOperationException("Initialization failed.")));

            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.AvailableCount, Is.EqualTo(1));
        }

        [Test]
        public void ReturnAll_ReturnsEveryActiveItem()
        {
            ObjectPool<TestItem> pool = CreatePool();
            pool.Rent();
            pool.Rent();

            pool.ReturnAll();

            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.AvailableCount, Is.EqualTo(2));
        }

        private static ObjectPool<TestItem> CreatePool(Action onCreate = null)
        {
            return new ObjectPool<TestItem>(
                () =>
                {
                    onCreate?.Invoke();
                    return new TestItem();
                },
                item => item.IsActive = false);
        }

        private sealed class TestItem
        {
            public bool IsActive { get; set; }
        }
    }
}
