using System;
using LastSeed.Core.Collections;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class OrderedReferenceSetTests
    {
        [Test]
        public void ReplaceWith_CopiesSourceAndBuildsMembershipLookup()
        {
            object first = new();
            object second = new();
            object[] source = { first, second };
            OrderedReferenceSet<object> set = new();

            set.ReplaceWith(source);
            source[0] = new object();

            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set.Items[0], Is.SameAs(first));
            Assert.That(set.Contains(first), Is.True);
            Assert.That(set.Contains(source[0]), Is.False);
        }

        [Test]
        public void ReplaceWith_InvalidCollection_PreservesCurrentItems()
        {
            object current = new();
            object duplicate = new();
            OrderedReferenceSet<object> set = new();
            set.ReplaceWith(new[] { current });

            Assert.Throws<ArgumentException>(() =>
                set.ReplaceWith(new[] { duplicate, duplicate }));

            Assert.That(set.Count, Is.EqualTo(1));
            Assert.That(set.Items[0], Is.SameAs(current));
            Assert.That(set.Contains(current), Is.True);
        }

        [Test]
        public void RemoveAll_ReturnsCountAndOriginalFirstIndex()
        {
            object first = new();
            object removedA = new();
            object middle = new();
            object removedB = new();
            OrderedReferenceSet<object> set = new();
            set.ReplaceWith(new[] { first, removedA, middle, removedB });

            int removedCount = set.RemoveAll(
                new[] { removedB, removedA, removedA },
                out int firstRemovedIndex);

            Assert.That(removedCount, Is.EqualTo(2));
            Assert.That(firstRemovedIndex, Is.EqualTo(1));
            Assert.That(set.Items, Is.EqualTo(new[] { first, middle }));
            Assert.That(set.Contains(removedA), Is.False);
            Assert.That(set.Contains(middle), Is.True);
        }

        [Test]
        public void Clear_RemovesItemsAndLookupState()
        {
            object item = new();
            OrderedReferenceSet<object> set = new();
            set.ReplaceWith(new[] { item });

            set.Clear();

            Assert.That(set.Count, Is.Zero);
            Assert.That(set.Contains(item), Is.False);
        }
    }
}
