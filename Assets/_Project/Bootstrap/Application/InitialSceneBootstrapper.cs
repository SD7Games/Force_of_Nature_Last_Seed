using System.Threading;
using LastSeed.Infrastructure.Navigation;
using UnityEngine;

namespace LastSeed.Bootstrap.Application
{
    public sealed class InitialSceneBootstrapper
    {
        private readonly UnitySceneLoader _sceneLoader;
        private readonly SceneLoadReadinessMonitor _readinessMonitor;

        public InitialSceneBootstrapper(
            UnitySceneLoader sceneLoader,
            SceneLoadReadinessMonitor readinessMonitor)
        {
            _sceneLoader = sceneLoader;
            _readinessMonitor = readinessMonitor;
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

            await _readinessMonitor.WaitAsync(
                loadOperation,
                cancellationToken);

            _sceneLoader.Activate(loadOperation);
        }

        public void Tick()
        {
            _readinessMonitor.Tick();
        }

        public void Cancel()
        {
            _readinessMonitor.Cancel();
        }
    }
}
