using UnityEngine;

namespace LastSeed.Infrastructure.Navigation
{
    public sealed class SceneNavigationService : ISceneNavigationService
    {
        private readonly UnitySceneLoader _sceneLoader;
        private AsyncOperation _activeLoadOperation;

        public SceneNavigationService(UnitySceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public bool IsLoading => _activeLoadOperation != null;

        public bool TryLoadLobbyScene()
        {
            return TryLoadScene(GameSceneNames.Lobby);
        }

        public bool TryLoadGameplayScene()
        {
            return TryLoadScene(GameSceneNames.Gameplay);
        }

        private bool TryLoadScene(string sceneName)
        {
            if (IsLoading)
                return false;

            Time.timeScale = 1f;

            AsyncOperation loadOperation = _sceneLoader.LoadAsync(
                sceneName,
                allowSceneActivation: true);

            if (loadOperation == null)
                return false;

            _activeLoadOperation = loadOperation;
            _activeLoadOperation.completed += HandleSceneLoadCompleted;
            return true;
        }

        private void HandleSceneLoadCompleted(AsyncOperation completedOperation)
        {
            completedOperation.completed -= HandleSceneLoadCompleted;

            if (_activeLoadOperation == completedOperation)
                _activeLoadOperation = null;
        }
    }
}
