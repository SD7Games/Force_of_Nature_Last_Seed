using System;

public sealed class WormSectionHealth
{
    public event Action Changed;
    public event Action Destroyed;

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

        CurrentHp = Math.Max(0, CurrentHp - damage);
        Changed?.Invoke();

        if (IsDestroyed)
            Destroyed?.Invoke();
    }

    private void SetHp(int hp, bool notify)
    {
        int clampedHp = Math.Max(1, hp);
        MaxHp = clampedHp;
        CurrentHp = clampedHp;

        if (notify)
            Changed?.Invoke();
    }
}
