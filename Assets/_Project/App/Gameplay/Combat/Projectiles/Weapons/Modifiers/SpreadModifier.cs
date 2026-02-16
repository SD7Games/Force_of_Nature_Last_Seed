using System.Collections.Generic;
using UnityEngine;

public class SpreadModifier : IShotModifier
{
    public void Apply(List<ShotSpawnData> shots, ShotContext context)
    {
        int count = Mathf.Max(1, context.Config.ProjectilesPerShot);
        float angle = context.Config.SpreadAngle;

        if (count <= 1 || angle <= 0f) return;

        shots.Clear();

        float half = angle * 0.5f;
        float step = angle / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float offset = -half + step * i;
            Quaternion rotation = context.FirePoint.rotation * Quaternion.Euler(0f, 0f, offset);

            shots.Add(new ShotSpawnData(context.FirePoint.position, rotation));
        }
    }

}