public readonly struct DamageInfo
{
    public readonly int Amount;
    public readonly DamageKind Kind;
    public readonly bool IsCritical;

    public DamageInfo(
        int amount,
        DamageKind kind = DamageKind.Normal,
        bool isCritical = false)
    {
        Amount = amount;
        Kind = kind;
        IsCritical = isCritical;
    }
}
