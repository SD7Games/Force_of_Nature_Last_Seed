using System.Collections;
using LastSeed.Infrastructure.Navigation;
using UnityEngine;

namespace LastSeed.Bootstrap.Application
{
    public sealed class InitialSceneBootstrapper
    {
        private readonly UnitySceneLoader _sceneLoader;

        public InitialSceneBootstrapper(UnitySceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public IEnumerator LoadInitialLobby(BootstrapLoadingView loadingView)
        {
            AsyncOperation loadOperation = _sceneLoader.LoadLobbyAsync(
                allowSceneActivation: false);

            if (loadOperation == null)
                yield break;

            if (loadingView != null)
                loadingView.Play();

            while (loadingView != null && !loadingView.IsComplete)
                yield return null;

            while (!_sceneLoader.IsReadyToActivate(loadOperation))
                yield return null;

            _sceneLoader.Activate(loadOperation);
        }
    }
}
