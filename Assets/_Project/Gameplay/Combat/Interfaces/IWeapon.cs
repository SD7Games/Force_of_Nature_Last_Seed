using UnityEngine;

public interface IWeapon
{
    void Init(ProjectilePool pool, Transform firePoint);

    void Tick(float deltaTime);

    void ApplyConfig(WeaponConfig config);
}
