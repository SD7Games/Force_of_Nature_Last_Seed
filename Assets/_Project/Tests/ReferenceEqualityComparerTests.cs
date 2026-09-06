using LastSeed.Core.Collections;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class ReferenceEqualityComparerTests
    {
        [Test]
        public void Equals_UsesIdentityWhenObjectsOverrideValueEquality()
        {
            EqualValue first = new(1);
            EqualValue second = new(1);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(
                ReferenceEqualityComparer<EqualValue>.Instance.Equals(first, second),
                Is.False);
            Assert.That(
                ReferenceEqualityComparer<EqualValue>.Instance.Equals(first, first),
                Is.True);
        }

        private sealed class EqualValue
        {
            private readonly int _value;

            public EqualValue(int value)
            {
                _value = value;
            }

            public override bool Equals(object other)
            {
                return other is EqualValue value && value._value == _value;
            }

            public override int GetHashCode()
            {
                return _value;
            }
        }
    }
}
