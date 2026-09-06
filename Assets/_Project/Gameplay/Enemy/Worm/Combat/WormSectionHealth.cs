using System;

public sealed class WormSectionHealth
{
    public event Action<WormSectionHealthChange> Changed;
    public event Action<WormSectionHealthChange> Destroyed;

    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public bool IsDestroyed => CurrentHp <= 0;
    public bool HasTakenDamage => CurrentHp < MaxHp;

    public void Initialize(int hp)
    {
        SetHp(hp, notify: false);
    }

    public void ResetHp(int hp)
    {
        SetHp(hp, notify: true);
    }

    public void ApplyDamage(int damage)
    {
        if (IsDestroyed || damage <= 0)
            return;

        int previousHp = CurrentHp;
        CurrentHp = Math.Max(0, CurrentHp - damage);
        WormSectionHealthChange change = new(
            previousHp,
            CurrentHp,
            MaxHp,
            previousHp - CurrentHp,
            isReset: false);
        Changed?.Invoke(change);

        if (IsDestroyed)
            Destroyed?.Invoke(change);
    }

    private void SetHp(int hp, bool notify)
    {
        int previousHp = CurrentHp;
        int clampedHp = Math.Max(1, hp);
        MaxHp = clampedHp;
        CurrentHp = clampedHp;

        if (notify)
        {
            Changed?.Invoke(new WormSectionHealthChange(
                previousHp,
                CurrentHp,
                MaxHp,
                appliedDamage: 0,
                isReset: true));
        }
    }
}
