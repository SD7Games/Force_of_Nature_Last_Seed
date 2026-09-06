using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LastSeed.Core.Collections
{
    public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static ReferenceEqualityComparer<T> Instance { get; } = new();

        private ReferenceEqualityComparer()
        {
        }

        public bool Equals(T left, T right)
        {
            return ReferenceEquals(left, right);
        }

        public int GetHashCode(T value)
        {
            return RuntimeHelpers.GetHashCode(value);
        }
    }
}
