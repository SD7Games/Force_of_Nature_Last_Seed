using System;

public static class WeaponDamageClamp
{
    public const int MaximumDamage = 9999999;

    public static int Clamp(double rawDamage)
    {
        if (double.IsNaN(rawDamage) || rawDamage <= 1d)
            return 1;

        if (rawDamage >= MaximumDamage)
            return MaximumDamage;

        return Math.Max(1, (int)Math.Round(rawDamage));
    }
}
