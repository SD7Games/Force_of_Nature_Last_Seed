using System;
using UnityEngine;
using Zenject;

namespace LastSeed.Bootstrap.Gameplay
{
    public sealed class GameSceneInitializer : IInitializable
    {
        private readonly Camera _worldCamera;
        private readonly ScreenBoundsService _screenBoundsService;
        private readonly PlayerMovementController _playerMovementController;
        private readonly PoolRegistry _projectilePoolRegistry;
        private readonly PlayerWeaponController _playerWeaponController;

        public GameSceneInitializer(
            Camera worldCamera,
            ScreenBoundsService screenBoundsService,
            PlayerMovementController playerMovementController,
            PoolRegistry projectilePoolRegistry,
            PlayerWeaponController playerWeaponController)
        {
            _worldCamera = worldCamera;
            _screenBoundsService = screenBoundsService;
            _playerMovementController = playerMovementController;
            _projectilePoolRegistry = projectilePoolRegistry;
            _playerWeaponController = playerWeaponController;
        }

        public void Initialize()
        {
            ValidateDependencies();

            _screenBoundsService.Recalculate(_worldCamera);
            _playerMovementController.Initialize(_screenBoundsService);
            _projectilePoolRegistry.Init(_screenBoundsService);
            _playerWeaponController.Initialize(_screenBoundsService);
        }

        private void ValidateDependencies()
        {
            if (_worldCamera == null)
                throw new InvalidOperationException("Game world camera is not configured.");

            if (_playerMovementController == null)
                throw new InvalidOperationException("Player movement controller is not configured.");

            if (_projectilePoolRegistry == null)
                throw new InvalidOperationException("Projectile pool registry is not configured.");

            if (_playerWeaponController == null)
                throw new InvalidOperationException("Player weapon controller is not configured.");
        }
    }
}
