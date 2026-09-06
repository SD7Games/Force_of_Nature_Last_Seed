using System;

namespace LastSeed.Core.Combat
{
    public sealed class Health
    {
        public event Action<HealthChange> Changed;
        public event Action<HealthChange> Depleted;

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public bool IsDepleted => CurrentHp <= 0;
        public bool HasTakenDamage => CurrentHp < MaxHp;

        public void Initialize(int hp)
        {
            SetHp(hp, notify: false);
        }

        public void Reset(int hp)
        {
            SetHp(hp, notify: true);
        }

        public void ApplyDamage(int damage)
        {
            if (IsDepleted || damage <= 0)
                return;

            int previousHp = CurrentHp;
            CurrentHp = Math.Max(0, CurrentHp - damage);
            HealthChange change = new(
                previousHp,
                CurrentHp,
                MaxHp,
                previousHp - CurrentHp,
                isReset: false);
            Changed?.Invoke(change);

            if (IsDepleted)
                Depleted?.Invoke(change);
        }

        private void SetHp(int hp, bool notify)
        {
            int previousHp = CurrentHp;
            int clampedHp = Math.Max(1, hp);
            MaxHp = clampedHp;
            CurrentHp = clampedHp;

            if (notify)
            {
                Changed?.Invoke(new HealthChange(
                    previousHp,
                    CurrentHp,
                    MaxHp,
                    appliedDamage: 0,
                    isReset: true));
            }
        }
    }
}
