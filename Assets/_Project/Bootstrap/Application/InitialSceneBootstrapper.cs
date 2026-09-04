using System.Threading;
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

        public async Awaitable LoadInitialLobbyAsync(
            BootstrapLoadingView loadingView,
            CancellationToken cancellationToken)
        {
            AsyncOperation loadOperation = _sceneLoader.LoadLobbyAsync(
                allowSceneActivation: false);

            if (loadOperation == null)
                return;

            if (loadingView != null)
                await loadingView.PlayAsync();

            await _sceneLoader.WaitUntilReadyToActivateAsync(
                loadOperation,
                cancellationToken);

            _sceneLoader.Activate(loadOperation);
        }
    }
}
