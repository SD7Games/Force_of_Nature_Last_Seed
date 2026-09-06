using System;
using UnityEngine;

public sealed class WormSectionHpViewPool
{
    private readonly WormSectionHpView _prefab;
    private readonly Transform _root;
    private readonly ObjectPool<WormSectionHpView> _pool;

    public WormSectionHpViewPool(WormSectionHpView prefab, Transform root)
    {
        _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
        _root = root != null ? root : throw new ArgumentNullException(nameof(root));
        _pool = new ObjectPool<WormSectionHpView>(CreateView, DeactivateView);
    }

    public WormSectionHpView Rent(Transform target, int currentHp)
    {
        Binding binding = new(target, currentHp);
        return _pool.Rent(binding, BindView);
    }

    public bool Return(WormSectionHpView view)
    {
        return _pool.Return(view);
    }

    private WormSectionHpView CreateView()
    {
        return UnityEngine.Object.Instantiate(_prefab, _root);
    }

    private static void BindView(WormSectionHpView view, in Binding binding)
    {
        view.gameObject.SetActive(true);
        view.Bind(binding.Target, binding.CurrentHp);
    }

    private static void DeactivateView(WormSectionHpView view)
    {
        view.Unbind();
        view.gameObject.SetActive(false);
    }

    private readonly struct Binding
    {
        public Binding(Transform target, int currentHp)
        {
            Target = target;
            CurrentHp = currentHp;
        }

        public Transform Target { get; }
        public int CurrentHp { get; }
    }
}
