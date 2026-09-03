using System;
using System.Collections.Generic;

public sealed class ObjectPool<T>
    where T : class
{
    private readonly Func<T> _create;
    private readonly Action<T> _onReturn;
    private readonly Queue<T> _available = new();
    private readonly List<T> _activeInRentOrder = new();
    private readonly HashSet<T> _active = new();

    public ObjectPool(Func<T> create, Action<T> onReturn)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _onReturn = onReturn ?? throw new ArgumentNullException(nameof(onReturn));
    }

    public int ActiveCount => _active.Count;
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

        if (!_active.Add(item))
            throw new InvalidOperationException("Pool attempted to rent an already active item.");

        _activeInRentOrder.Add(item);
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

    public bool Return(T item)
    {
        if (item == null || !_active.Contains(item))
            return false;

        _onReturn(item);
        _active.Remove(item);
        _activeInRentOrder.Remove(item);
        _available.Enqueue(item);
        return true;
    }

    public void ReturnAll()
    {
        while (_activeInRentOrder.Count > 0)
        {
            int lastIndex = _activeInRentOrder.Count - 1;
            T item = _activeInRentOrder[lastIndex];
            Return(item);
        }
    }

    private T CreateItem()
    {
        T item = _create();

        return item ?? throw new InvalidOperationException("Pool factory returned null.");
    }
}
