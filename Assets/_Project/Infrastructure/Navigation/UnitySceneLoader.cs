using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSeed.Infrastructure.Navigation
{
    public sealed class UnitySceneLoader
    {
        private const float ReadyForActivationProgress = 0.9f;

        public AsyncOperation LoadLobbyAsync(bool allowSceneActivation)
        {
            return LoadAsync(GameSceneNames.Lobby, allowSceneActivation);
        }

        public AsyncOperation LoadGameplayAsync(bool allowSceneActivation)
        {
            return LoadAsync(GameSceneNames.Gameplay, allowSceneActivation);
        }

        public AsyncOperation LoadAsync(string sceneName, bool allowSceneActivation)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("UnitySceneLoader: scene name is empty.");
                return null;
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);

            if (loadOperation != null)
                loadOperation.allowSceneActivation = allowSceneActivation;
            else
                Debug.LogError($"UnitySceneLoader: failed to start loading scene '{sceneName}'.");

            return loadOperation;
        }

        public void Activate(AsyncOperation loadOperation)
        {
            if (loadOperation != null)
                loadOperation.allowSceneActivation = true;
        }

        public bool IsReadyToActivate(AsyncOperation loadOperation)
        {
            return loadOperation != null &&
                loadOperation.progress >= ReadyForActivationProgress;
        }
    }
}
