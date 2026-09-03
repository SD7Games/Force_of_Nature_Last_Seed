namespace LastSeed.Gameplay.Combat
{
    public interface ICombatSessionState
    {
        bool IsShootingEnabled { get; }

        void SetShootingEnabled(bool isEnabled);
        void Reset();
    }
}
