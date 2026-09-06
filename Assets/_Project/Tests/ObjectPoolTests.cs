using System;
using LastSeed.Core.Pooling;
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

        [Test]
        public void Rent_WithTypedState_InitializesWithoutCapturedClosure()
        {
            ObjectPool<TestItem> pool = CreatePool();
            int value = 42;

            TestItem item = pool.Rent(value, InitializeWithValue);

            Assert.That(item.Value, Is.EqualTo(42));
            Assert.That(pool.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void Return_WhenRemovingMiddleItem_KeepsSwapBackIndexConsistent()
        {
            ObjectPool<TestItem> pool = CreatePool();
            TestItem first = pool.Rent();
            TestItem middle = pool.Rent();
            TestItem last = pool.Rent();

            Assert.That(pool.Return(middle), Is.True);
            Assert.That(pool.Return(last), Is.True);
            Assert.That(pool.Return(first), Is.True);

            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.AvailableCount, Is.EqualTo(3));
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

        private static void InitializeWithValue(TestItem item, in int value)
        {
            item.Value = value;
        }

        private sealed class TestItem
        {
            public bool IsActive { get; set; }
            public int Value { get; set; }
        }
    }
}
