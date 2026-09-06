using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WormSection : IWormSectionHpTarget
{
    private readonly WormSectionHealth _health = new();
    private readonly List<WormSegment> _segments = new();

    public WormSection()
    {
        _health.Changed += HandleHealthChanged;
        _health.Destroyed += HandleDestroyed;
    }

    public event Action<WormSectionHealthChanged> HpChanged;
    public event Action<WormSectionDestroyed> Destroyed;

    public int MaxHp => _health.MaxHp;
    public int CurrentHp => _health.CurrentHp;
    public int Index { get; set; }
    public int HpOrder => GetCenterSegmentIndex();

    public CocoonRewardProfile CocoonProfile { get; private set; }
    public bool HasCocoon => CocoonProfile != null;
    public bool HasReward => HasCocoon;

    public IReadOnlyList<WormSegment> Segments => _segments;
    public bool IsDestroyed => _health.IsDestroyed;
    public bool HasTakenDamage => _health.HasTakenDamage;
    public bool HasVisibleAliveSegment => ContainsVisibleAliveSegment();

    public void InitializeHp(int hp)
    {
        _health.Initialize(hp);
    }

    public void ResetHp(int hp)
    {
        _health.ResetHp(hp);
    }

    private bool ContainsVisibleAliveSegment()
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            WormSegment segment = _segments[i];

            if (segment != null && segment.IsAlive && segment.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    public void SetCocoon(CocoonRewardProfile profile)
    {
        CocoonProfile = profile;
    }

    public void AddSegment(WormSegment segment)
    {
        if (segment == null)
            return;

        _segments.Add(segment);
        segment.Section = this;
    }

    public Transform GetHpAnchor()
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i].HasCocoon)
                return _segments[i].CachedTransform;
        }

        int centerIndex = _segments.Count / 2;
        return _segments[centerIndex].CachedTransform;
    }

    public int GetCenterSegmentIndex()
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i].HasCocoon)
                return _segments[i].Index;
        }

        int mid = _segments.Count / 2;
        return _segments[mid].Index;
    }

    public void Damage(int damage)
    {
        _health.ApplyDamage(damage);
    }

    public List<WormSegment> ReleaseSegments()
    {
        List<WormSegment> released = new(_segments.Count);

        for (int i = 0; i < _segments.Count; i++)
        {
            WormSegment segment = _segments[i];

            if (segment == null)
                continue;

            if (segment.Section == this)
                segment.Section = null;

            released.Add(segment);
        }

        _segments.Clear();
        return released;
    }

    private void HandleHealthChanged(WormSectionHealthChange change)
    {
        HpChanged?.Invoke(new WormSectionHealthChanged(this, change));
    }

    private void HandleDestroyed(WormSectionHealthChange finalChange)
    {
        Destroyed?.Invoke(new WormSectionDestroyed(this, finalChange));
    }
}
