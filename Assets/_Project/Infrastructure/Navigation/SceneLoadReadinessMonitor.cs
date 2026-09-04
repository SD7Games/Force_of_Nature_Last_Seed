using System;
using System.Threading;
using UnityEngine;

namespace LastSeed.Infrastructure.Navigation
{
    public sealed class SceneLoadReadinessMonitor
    {
        private readonly UnitySceneLoader _sceneLoader;
        private AsyncOperation _loadOperation;
        private CancellationToken _cancellationToken;
        private AwaitableCompletionSource _completionSource;

        public SceneLoadReadinessMonitor(UnitySceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
        }

        public Awaitable WaitAsync(
            AsyncOperation loadOperation,
            CancellationToken cancellationToken)
        {
            if (loadOperation == null)
                throw new ArgumentNullException(nameof(loadOperation));

            if (_completionSource != null)
                throw new InvalidOperationException("A scene readiness wait is already active.");

            AwaitableCompletionSource completionSource = new();
            _loadOperation = loadOperation;
            _cancellationToken = cancellationToken;
            _completionSource = completionSource;
            Tick();
            return completionSource.Awaitable;
        }

        public void Tick()
        {
            if (_completionSource == null)
                return;

            if (_cancellationToken.IsCancellationRequested)
            {
                Cancel();
                return;
            }

            if (!_sceneLoader.IsReadyToActivate(_loadOperation))
                return;

            AwaitableCompletionSource completionSource = Release();
            completionSource.TrySetResult();
        }

        public void Cancel()
        {
            AwaitableCompletionSource completionSource = Release();
            completionSource?.TrySetCanceled();
        }

        private AwaitableCompletionSource Release()
        {
            AwaitableCompletionSource completionSource = _completionSource;
            _completionSource = null;
            _loadOperation = null;
            _cancellationToken = default;
            return completionSource;
        }
    }
}
