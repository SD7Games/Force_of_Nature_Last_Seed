using System;
using System.Collections.Generic;

namespace LastSeed.Core.Collections
{
    public sealed class UniqueReferenceQueue<T>
        where T : class
    {
        private readonly LinkedList<T> _items = new();
        private readonly Dictionary<T, LinkedListNode<T>> _nodes =
            new(ReferenceEqualityComparer<T>.Instance);

        public int Count => _items.Count;

        public bool Contains(T item)
        {
            return item != null && _nodes.ContainsKey(item);
        }

        public bool Enqueue(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (_nodes.ContainsKey(item))
                return false;

            LinkedListNode<T> node = _items.AddLast(item);

            try
            {
                _nodes.Add(item, node);
                return true;
            }
            catch
            {
                _items.Remove(node);
                throw;
            }
        }

        public bool TryDequeue(out T item)
        {
            LinkedListNode<T> node = _items.First;

            if (node == null)
            {
                item = null;
                return false;
            }

            item = node.Value;
            _items.RemoveFirst();
            _nodes.Remove(item);
            return true;
        }

        public bool Remove(T item)
        {
            if (item == null || !_nodes.TryGetValue(item, out LinkedListNode<T> node))
                return false;

            _items.Remove(node);
            _nodes.Remove(item);
            return true;
        }

        public void Clear()
        {
            _items.Clear();
            _nodes.Clear();
        }
    }
}
