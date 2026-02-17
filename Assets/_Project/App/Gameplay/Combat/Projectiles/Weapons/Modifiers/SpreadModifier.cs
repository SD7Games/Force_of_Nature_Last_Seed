using System.Collections.Generic;
using UnityEngine;

public class SpreadModifier : IShotModifier
{
    private readonly int _count;
    private readonly float _angle;

    public SpreadModifier(int count, float angle)
    {
        _count = count;
        _angle = angle;
    }

    public void Apply(List<ShotSpawnData> shots, ShotContext context)
    {

        if (_count <= 1 || _angle <= 0f) return;

        shots.Clear();

        float half = _angle * 0.5f;
        float step = _angle / (_count - 1);

        for (int i = 0; i < _count; i++)
        {
            float offset = -half + step * i;
            Quaternion rotation = context.FirePoint.rotation * Quaternion.Euler(0f, 0f, offset);

            shots.Add(new ShotSpawnData(context.FirePoint.position, rotation));
        }
    }

}