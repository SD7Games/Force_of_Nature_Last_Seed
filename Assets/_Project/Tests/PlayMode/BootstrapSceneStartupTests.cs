using System.Collections;
using LastSeed.Infrastructure.Navigation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;

namespace LastSeed.Tests.PlayMode
{
    public sealed class BootstrapSceneStartupTests
    {
        private const float LobbyLoadTimeoutSeconds = 10f;

        [UnityTest]
        public IEnumerator BootstrapScene_LoadsLobbyThroughApplicationEntryPoint()
        {
            AsyncOperation bootstrapLoad = SceneManager.LoadSceneAsync(
                GameSceneNames.Bootstrap,
                LoadSceneMode.Single);

            Assert.That(bootstrapLoad, Is.Not.Null);
            yield return bootstrapLoad;

            float deadline = Time.realtimeSinceStartup + LobbyLoadTimeoutSeconds;

            while (SceneManager.GetActiveScene().name != GameSceneNames.Lobby
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.EqualTo(GameSceneNames.Lobby));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (ProjectContext.HasInstance)
                Object.Destroy(ProjectContext.Instance.gameObject);

            yield return null;
        }
    }
}
