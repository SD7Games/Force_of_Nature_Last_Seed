using System;
using UnityEngine;
using Zenject;

namespace LastSeed.Bootstrap.Installers
{
    public sealed class PlayerInstaller : MonoInstaller
    {
        [SerializeField] private PlayerMover _playerMover;
        [SerializeField] private ProjectileWeapon _projectileWeapon;
        [SerializeField] private AcaciaThornWeapon _acaciaThornWeapon;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private WeaponConfig _startWeaponConfig;

#if UNITY_EDITOR
        public WeaponConfig EditorStartWeaponConfig => _startWeaponConfig;
#endif

        public override void InstallBindings()
        {
            ValidateReferences();

            Container.Bind<PlayerMover>().FromInstance(_playerMover).AsSingle();
            Container.Bind<ProjectileWeapon>().FromInstance(_projectileWeapon).AsSingle();
            Container.Bind<AcaciaThornWeapon>().FromInstance(_acaciaThornWeapon).AsSingle();
            Container.BindInstance(new PlayerWeaponLoadout(_firePoint, _startWeaponConfig)).AsSingle();
            Container.Bind<PlayerMovementController>().AsSingle();
            Container.Bind<PlayerWeaponController>().AsSingle();
        }

        private void ValidateReferences()
        {
            if (_playerMover == null)
                throw new InvalidOperationException("Player mover is not configured.");

            if (_projectileWeapon == null)
                throw new InvalidOperationException("Projectile weapon is not configured.");

            if (_acaciaThornWeapon == null)
                throw new InvalidOperationException("Acacia Thorn weapon is not configured.");

            if (_firePoint == null)
                throw new InvalidOperationException("Player fire point is not configured.");

            if (_startWeaponConfig == null)
                throw new InvalidOperationException("Player start weapon config is not configured.");
        }
    }
}
