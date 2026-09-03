public sealed class WeaponPowerProvider : IWeaponPowerProvider
{
    private readonly ProjectileWeapon _mainWeapon;
    private readonly AcaciaThornWeapon _acaciaThornWeapon;

    public WeaponPowerProvider(
        ProjectileWeapon mainWeapon,
        AcaciaThornWeapon acaciaThornWeapon)
    {
        _mainWeapon = mainWeapon;
        _acaciaThornWeapon = acaciaThornWeapon;
    }

    public WeaponPowerSnapshot GetCurrentPower()
    {
        return WeaponPowerEstimator.Estimate(_mainWeapon, _acaciaThornWeapon);
    }
}
