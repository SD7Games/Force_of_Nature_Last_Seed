using System;
using System.Collections.Generic;
using LastSeed.Core.Collections;

namespace LastSeed.Core.Pooling
{
    public sealed class ObjectPool<T>
        where T : class
    {
        public delegate void ItemInitializer<TState>(T item, in TState state);

        private readonly Func<T> _create;
        private readonly Action<T> _onReturn;
        private readonly Queue<T> _available = new();
        private readonly List<T> _activeItems = new();
        private readonly Dictionary<T, int> _activeIndices =
            new(ReferenceEqualityComparer<T>.Instance);

        public ObjectPool(Func<T> create, Action<T> onReturn)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _onReturn = onReturn ?? throw new ArgumentNullException(nameof(onReturn));
        }

        public int ActiveCount => _activeItems.Count;
        public int AvailableCount => _available.Count;

        public void Prewarm(int count)
        {
            for (int index = 0; index < Math.Max(0, count); index++)
                PrewarmOne();
        }

        public void PrewarmOne()
        {
            T item = CreateItem();
            _onReturn(item);
            _available.Enqueue(item);
        }

        public T Rent()
        {
            T item = _available.Count > 0
                ? _available.Dequeue()
                : CreateItem();

            if (_activeIndices.ContainsKey(item))
                throw new InvalidOperationException("Pool attempted to rent an already active item.");

            int activeIndex = _activeItems.Count;

            try
            {
                _activeIndices.Add(item, activeIndex);
                _activeItems.Add(item);
            }
            catch
            {
                _activeIndices.Remove(item);
                _onReturn(item);
                _available.Enqueue(item);
                throw;
            }

            return item;
        }

        public T Rent(Action<T> initialize)
        {
            if (initialize == null)
                throw new ArgumentNullException(nameof(initialize));

            T item = Rent();

            try
            {
                initialize(item);
                return item;
            }
            catch
            {
                Return(item);
                throw;
            }
        }

        public T Rent<TState>(
            in TState state,
            ItemInitializer<TState> initialize)
        {
            if (initialize == null)
                throw new ArgumentNullException(nameof(initialize));

            T item = Rent();

            try
            {
                initialize(item, state);
                return item;
            }
            catch
            {
                Return(item);
                throw;
            }
        }

        public bool Return(T item)
        {
            if (item == null || !_activeIndices.TryGetValue(item, out int activeIndex))
                return false;

            _onReturn(item);
            RemoveActiveAtSwapBack(activeIndex);
            _available.Enqueue(item);
            return true;
        }

        public void ReturnAll()
        {
            int returnCount = _activeItems.Count;

            for (int index = 0; index < returnCount; index++)
            {
                int lastIndex = _activeItems.Count - 1;
                T item = _activeItems[lastIndex];
                Return(item);
            }
        }

        private void RemoveActiveAtSwapBack(int activeIndex)
        {
            int lastIndex = _activeItems.Count - 1;
            T removedItem = _activeItems[activeIndex];
            T lastItem = _activeItems[lastIndex];

            if (activeIndex != lastIndex)
            {
                _activeItems[activeIndex] = lastItem;
                _activeIndices[lastItem] = activeIndex;
            }

            _activeItems.RemoveAt(lastIndex);
            _activeIndices.Remove(removedItem);
        }

        private T CreateItem()
        {
            T item = _create();

            return item ?? throw new InvalidOperationException("Pool factory returned null.");
        }
    }
}
