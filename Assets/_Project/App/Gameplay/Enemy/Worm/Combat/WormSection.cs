using System.Collections.Generic;

public sealed class WormSection
{
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }

    public readonly List<WormSegment> Segments = new();

    public bool IsDestroyed => CurrentHP <= 0;

    public void Init(int hp)
    {
        MaxHP = hp;
        CurrentHP = hp;
    }

    public void AddSegment(WormSegment segment)
    {
        Segments.Add(segment);
    }

    public void Damage(int damage)
    {
        CurrentHP -= damage;
    }
}