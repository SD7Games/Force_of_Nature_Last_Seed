using System;
using System.Collections.Generic;
using LastSeed.Infrastructure.Navigation;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class SceneRouteCatalogTests
    {
        [Test]
        public void GetSceneName_ReturnsRegisteredRoute()
        {
            SceneRouteCatalog<TestScene> catalog = CreateCatalog();

            Assert.That(catalog.Count, Is.EqualTo(2));
            Assert.That(catalog.GetSceneName(TestScene.Gameplay), Is.EqualTo("Game"));
        }

        [Test]
        public void Constructor_RejectsDuplicateKeys()
        {
            SceneRoute<TestScene>[] routes =
            {
                new(TestScene.Lobby, "Lobby"),
                new(TestScene.Lobby, "LobbyCopy")
            };

            Assert.Throws<ArgumentException>(() =>
                new SceneRouteCatalog<TestScene>(routes));
        }

        [Test]
        public void GetSceneName_RejectsUnknownRoute()
        {
            SceneRouteCatalog<TestScene> catalog = CreateCatalog();

            Assert.Throws<KeyNotFoundException>(() =>
                catalog.GetSceneName((TestScene)99));
        }

        [Test]
        public void GameSceneRoutes_RegisterEveryStableSceneId()
        {
            SceneRouteCatalog<GameSceneId> catalog = GameSceneRoutes.CreateCatalog();
            GameSceneId[] sceneIds = (GameSceneId[])Enum.GetValues(typeof(GameSceneId));

            Assert.That(catalog.Count, Is.EqualTo(sceneIds.Length));

            for (int i = 0; i < sceneIds.Length; i++)
                Assert.That(catalog.GetSceneName(sceneIds[i]), Is.Not.Empty);
        }

        private static SceneRouteCatalog<TestScene> CreateCatalog()
        {
            return new SceneRouteCatalog<TestScene>(new[]
            {
                new SceneRoute<TestScene>(TestScene.Lobby, "Lobby"),
                new SceneRoute<TestScene>(TestScene.Gameplay, "Game")
            });
        }

        private enum TestScene
        {
            Lobby,
            Gameplay
        }
    }
}
