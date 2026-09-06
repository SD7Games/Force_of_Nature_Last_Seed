using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSeed.Infrastructure.Navigation
{
    public sealed class UnitySceneLoader : ISceneLoader
    {
        public ISceneLoadOperation BeginLoad(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new ArgumentException("Scene name cannot be empty.", nameof(sceneName));

            Time.timeScale = 1f;

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single);

            if (loadOperation == null)
                throw new InvalidOperationException(
                    $"Unity failed to start loading scene '{sceneName}'.");

            loadOperation.allowSceneActivation = false;
            return new UnitySceneLoadOperation(loadOperation);
        }
    }
}
