using System;
using System.Collections.Generic;

namespace LastSeed.Core.Collections
{
    public sealed class OrderedReferenceSet<T>
        where T : class
    {
        private readonly List<T> _items = new();
        private readonly HashSet<T> _removalLookup =
            new(ReferenceEqualityComparer<T>.Instance);
        private HashSet<T> _membership =
            new(ReferenceEqualityComparer<T>.Instance);
        private HashSet<T> _replacementMembership =
            new(ReferenceEqualityComparer<T>.Instance);

        public int Count => _items.Count;
        public IReadOnlyList<T> Items => _items;

        public bool Contains(T item)
        {
            return item != null && _membership.Contains(item);
        }

        public void ReplaceWith(IReadOnlyList<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            BuildReplacementMembership(items);
            _items.Clear();

            if (_items.Capacity < items.Count)
                _items.Capacity = items.Count;

            for (int index = 0; index < items.Count; index++)
                _items.Add(items[index]);

            HashSet<T> previousMembership = _membership;
            _membership = _replacementMembership;
            _replacementMembership = previousMembership;
            _replacementMembership.Clear();
            _removalLookup.Clear();
        }

        public void Clear()
        {
            _items.Clear();
            _membership.Clear();
            _replacementMembership.Clear();
            _removalLookup.Clear();
        }

        public int RemoveAll(
            IReadOnlyList<T> removedItems,
            out int firstRemovedIndex)
        {
            firstRemovedIndex = -1;

            if (removedItems == null || removedItems.Count == 0)
                return 0;

            BuildRemovalLookup(removedItems);
            int writeIndex = 0;

            for (int readIndex = 0; readIndex < _items.Count; readIndex++)
            {
                T item = _items[readIndex];

                if (_removalLookup.Contains(item))
                {
                    if (firstRemovedIndex < 0)
                        firstRemovedIndex = readIndex;

                    _membership.Remove(item);
                    continue;
                }

                if (writeIndex != readIndex)
                    _items[writeIndex] = item;

                writeIndex++;
            }

            int removedCount = _items.Count - writeIndex;

            if (removedCount > 0)
                _items.RemoveRange(writeIndex, removedCount);

            _removalLookup.Clear();
            return removedCount;
        }

        private void BuildReplacementMembership(IReadOnlyList<T> items)
        {
            _replacementMembership.Clear();

            for (int index = 0; index < items.Count; index++)
            {
                T item = items[index];

                if (item == null)
                {
                    _replacementMembership.Clear();
                    throw new ArgumentException("Collection contains null.", nameof(items));
                }

                if (_replacementMembership.Add(item))
                    continue;

                _replacementMembership.Clear();
                throw new ArgumentException("Collection contains duplicates.", nameof(items));
            }
        }

        private void BuildRemovalLookup(IReadOnlyList<T> removedItems)
        {
            _removalLookup.Clear();

            for (int index = 0; index < removedItems.Count; index++)
            {
                T item = removedItems[index];

                if (item != null)
                    _removalLookup.Add(item);
            }
        }
    }
}
