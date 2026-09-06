using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LastSeed.Infrastructure.Navigation
{
    public sealed class SceneNavigator<TScene> : ISceneNavigator<TScene>
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly SceneRouteCatalog<TScene> _routeCatalog;
        private ISceneLoadOperation _activeLoadOperation;

        public SceneNavigator(
            ISceneLoader sceneLoader,
            SceneRouteCatalog<TScene> routeCatalog)
        {
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _routeCatalog = routeCatalog ?? throw new ArgumentNullException(nameof(routeCatalog));
        }

        public bool IsNavigating => _activeLoadOperation != null;

        public UniTask<bool> TryNavigateAsync(
            TScene scene,
            CancellationToken cancellationToken)
        {
            return TryNavigateAsync(scene, null, cancellationToken);
        }

        public async UniTask<bool> TryNavigateAsync(
            TScene scene,
            ISceneTransition transition,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsNavigating)
                return false;

            string sceneName = _routeCatalog.GetSceneName(scene);
            ISceneLoadOperation loadOperation = _sceneLoader.BeginLoad(sceneName);

            if (loadOperation == null)
                throw new InvalidOperationException(
                    $"Scene loader did not create an operation for '{sceneName}'.");

            _activeLoadOperation = loadOperation;
            bool activationRequested = false;

            try
            {
                UniTask readinessTask = loadOperation.WaitUntilReadyAsync(cancellationToken);
                UniTask transitionTask = transition != null
                    ? transition.PlayAsync(cancellationToken)
                    : UniTask.CompletedTask;

                await UniTask.WhenAll(readinessTask, transitionTask);
                cancellationToken.ThrowIfCancellationRequested();

                loadOperation.Activate();
                activationRequested = true;
                await loadOperation.WaitUntilCompletedAsync(CancellationToken.None);
                return true;
            }
            catch
            {
                if (!activationRequested)
                    loadOperation.Activate();

                await CompleteFailedTransitionAsync(loadOperation);
                throw;
            }
            finally
            {
                _activeLoadOperation = null;
            }
        }

        private static async UniTask CompleteFailedTransitionAsync(
            ISceneLoadOperation loadOperation)
        {
            try
            {
                await loadOperation.WaitUntilCompletedAsync(CancellationToken.None);
            }
            catch
            {
            }
        }
    }
}
