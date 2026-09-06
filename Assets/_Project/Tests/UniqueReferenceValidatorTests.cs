using System;
using LastSeed.Core.Collections;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class UniqueReferenceValidatorTests
    {
        [Test]
        public void Validate_WithUniqueItems_Completes()
        {
            object[] items = { new(), new() };

            Assert.DoesNotThrow(() => UniqueReferenceValidator.Validate(items, nameof(items)));
        }

        [Test]
        public void Validate_WithNullItem_ThrowsArgumentException()
        {
            object[] items = { new(), null };

            Assert.Throws<ArgumentException>(() =>
                UniqueReferenceValidator.Validate(items, nameof(items)));
        }

        [Test]
        public void Validate_WithDuplicateItem_ThrowsArgumentException()
        {
            object item = new();
            object[] items = { item, item };

            Assert.Throws<ArgumentException>(() =>
                UniqueReferenceValidator.Validate(items, nameof(items)));
        }
    }
}
