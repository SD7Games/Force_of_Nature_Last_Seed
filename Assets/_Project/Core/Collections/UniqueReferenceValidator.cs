using System;
using System.Collections.Generic;

namespace LastSeed.Core.Collections
{
    public static class UniqueReferenceValidator
    {
        public static void Validate<T>(IReadOnlyList<T> items, string parameterName)
            where T : class
        {
            if (items == null)
                throw new ArgumentNullException(parameterName);

            HashSet<T> uniqueItems = new(
                items.Count,
                ReferenceEqualityComparer<T>.Instance);

            for (int index = 0; index < items.Count; index++)
            {
                T item = items[index];

                if (item == null)
                    throw new ArgumentException("Collection contains null.", parameterName);

                if (!uniqueItems.Add(item))
                    throw new ArgumentException("Collection contains duplicates.", parameterName);
            }
        }
    }
}
