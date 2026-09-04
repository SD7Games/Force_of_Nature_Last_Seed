using UnityEngine;

public sealed class UnityRandomSource : IRandomSource
{
    public float NextUnitFloat()
    {
        return Random.value;
    }
}
