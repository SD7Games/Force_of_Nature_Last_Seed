using UnityEngine;
using UnityEngine.InputSystem;

public sealed class DevModifierChecker : MonoBehaviour
{
    [SerializeField] private ProjectileWeapon _weapon;
    [SerializeField] private WeaponConfig _config;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            _weapon.ApplyConfig(_config);
        }
    }
}