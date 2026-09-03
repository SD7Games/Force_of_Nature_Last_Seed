using System.Collections;
using LastSeed.Bootstrap.GameplayLoop;
using LastSeed.Infrastructure.Input;
using LastSeed.Infrastructure.Navigation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;

namespace LastSeed.Tests.PlayMode
{
    public sealed class GameplaySceneStartupTests
    {
        private const int InitializationFrameCount = 2;

        [UnityTest]
        public IEnumerator GameplayScene_WhenLoadedDirectly_ResolvesRequiredRuntimeDependencies()
        {
            AsyncOperation sceneLoadOperation = SceneManager.LoadSceneAsync(
                GameSceneNames.Gameplay,
                LoadSceneMode.Single);

            Assert.That(sceneLoadOperation, Is.Not.Null);
            yield return sceneLoadOperation;

            for (int frameIndex = 0; frameIndex < InitializationFrameCount; frameIndex++)
                yield return null;

            Scene gameplayScene = SceneManager.GetActiveScene();

            Assert.That(gameplayScene.name, Is.EqualTo(GameSceneNames.Gameplay));
            Assert.That(ProjectContext.HasInstance, Is.True);
            Assert.That(FindInScene<SceneContext>(gameplayScene), Is.Not.Null);
            Assert.That(FindInScene<PlayerInputSnapshotProvider>(gameplayScene), Is.Not.Null);
            Assert.That(FindInScene<GameplayUpdateDriver>(gameplayScene), Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (ProjectContext.HasInstance)
                Object.Destroy(ProjectContext.Instance.gameObject);

            yield return null;
        }

        private static TComponent FindInScene<TComponent>(Scene scene)
            where TComponent : Component
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();

            for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
            {
                TComponent component = rootObjects[rootIndex].GetComponentInChildren<TComponent>(true);

                if (component != null)
                    return component;
            }

            return null;
        }
    }
}
