public interface IWormSectionHpTarget
{
    int Index { get; set; }
    int HpOrder { get; }
    int MaxHp { get; }
    bool IsDestroyed { get; }
    bool HasTakenDamage { get; }
    bool HasVisibleAliveSegment { get; }

    void InitializeHp(int hp);
    void ResetHp(int hp);
}
