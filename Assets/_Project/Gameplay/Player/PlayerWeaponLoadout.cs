using System;
using UnityEngine;

public sealed class PlayerWeaponLoadout
{
    public PlayerWeaponLoadout(Transform firePoint, WeaponConfig startConfig)
    {
        FirePoint = firePoint != null
            ? firePoint
            : throw new ArgumentNullException(nameof(firePoint));

        StartConfig = startConfig != null
            ? startConfig
            : throw new ArgumentNullException(nameof(startConfig));
    }

    public Transform FirePoint { get; }
    public WeaponConfig StartConfig { get; }
}
