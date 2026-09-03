using System;
using UnityEngine;
using Zenject;

namespace LastSeed.Bootstrap.Gameplay
{
    public sealed class GameWorldInitializer : IInitializable
    {
        private readonly Camera _worldCamera;
        private readonly ScreenBoundsService _screenBoundsService;
        private readonly PlayerMover _playerMover;
        private readonly PoolRegistry _projectilePoolRegistry;
        private readonly PlayerShooter _playerShooter;

        public GameWorldInitializer(
            Camera worldCamera,
            ScreenBoundsService screenBoundsService,
            PlayerMover playerMover,
            PoolRegistry projectilePoolRegistry,
            PlayerShooter playerShooter)
        {
            _worldCamera = worldCamera;
            _screenBoundsService = screenBoundsService;
            _playerMover = playerMover;
            _projectilePoolRegistry = projectilePoolRegistry;
            _playerShooter = playerShooter;
        }

        public void Initialize()
        {
            ValidateDependencies();

            _screenBoundsService.Recalculate(_worldCamera);
            _playerMover.Init(_screenBoundsService);
            _projectilePoolRegistry.Init(_screenBoundsService);
            _playerShooter.Init(_screenBoundsService);
        }

        private void ValidateDependencies()
        {
            if (_worldCamera == null)
                throw new InvalidOperationException("Game world camera is not configured.");

            if (_playerMover == null)
                throw new InvalidOperationException("Player mover is not configured.");

            if (_projectilePoolRegistry == null)
                throw new InvalidOperationException("Projectile pool registry is not configured.");

            if (_playerShooter == null)
                throw new InvalidOperationException("Player shooter is not configured.");
        }
    }
}
