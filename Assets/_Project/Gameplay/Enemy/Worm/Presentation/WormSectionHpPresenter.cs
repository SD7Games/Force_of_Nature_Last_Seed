using System;
using System.Collections.Generic;
using LastSeed.Core.Collections;
using UnityEngine;

public sealed class WormSectionHpPresenter : MonoBehaviour
{
    [SerializeField] private WormSectionHpView _viewPrefab;
    [SerializeField] private Transform _root;

    private readonly Dictionary<WormSection, WormSectionHpView> _views = new();
    private WormSectionHpViewPool _viewPool;

    private void Awake()
    {
        if (!ValidateReferences())
            return;

        _viewPool = new WormSectionHpViewPool(_viewPrefab, _root);
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnValidate()
    {
        ValidateReferences();
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (_viewPrefab == null)
        {
            Debug.LogError("WormSectionHpPresenter: View prefab is not assigned.", this);
            isValid = false;
        }

        if (_root == null)
        {
            Debug.LogError("WormSectionHpPresenter: Root is not assigned.", this);
            isValid = false;
        }

        return isValid;
    }

    public void BindSections(IReadOnlyList<WormSection> sections)
    {
        if (sections == null)
            throw new ArgumentNullException(nameof(sections));

        if (_viewPool == null)
            throw new InvalidOperationException("Worm section HP view pool is not initialized.");

        if (_views.Count > 0)
            return;

        UniqueReferenceValidator.Validate(sections, nameof(sections));

        try
        {
            for (int i = 0; i < sections.Count; i++)
                BindSection(sections[i]);
        }
        catch
        {
            Clear();
            throw;
        }
    }

    public void Clear()
    {
        foreach (KeyValuePair<WormSection, WormSectionHpView> entry in _views)
        {
            WormSection section = entry.Key;

            if (section != null)
            {
                section.HpChanged -= OnHpChanged;
                section.Destroyed -= OnSectionDestroyed;
            }

            _viewPool?.Return(entry.Value);
        }

        _views.Clear();
    }

    private void BindSection(WormSection section)
    {
        WormSectionHpView view = _viewPool.Rent(section.GetHpAnchor(), section.CurrentHp);

        try
        {
            section.HpChanged += OnHpChanged;
            section.Destroyed += OnSectionDestroyed;

            _views.Add(section, view);
        }
        catch
        {
            section.HpChanged -= OnHpChanged;
            section.Destroyed -= OnSectionDestroyed;
            _viewPool.Return(view);
            throw;
        }
    }

    private void OnHpChanged(WormSectionHealthChanged healthChanged)
    {
        if (!_views.TryGetValue(healthChanged.Section, out WormSectionHpView view))
            return;

        view.SetValue(healthChanged.Change.CurrentHp);
    }

    private void OnSectionDestroyed(WormSectionDestroyed destroyed)
    {
        WormSection section = destroyed.Section;

        if (!_views.TryGetValue(section, out WormSectionHpView view))
            return;

        section.HpChanged -= OnHpChanged;
        section.Destroyed -= OnSectionDestroyed;

        _views.Remove(section);
        _viewPool.Return(view);
    }
}
