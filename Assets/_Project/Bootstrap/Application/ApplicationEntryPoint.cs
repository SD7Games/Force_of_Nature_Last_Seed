using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace LastSeed.Bootstrap.Application
{
    [DisallowMultipleComponent]
    public sealed class ApplicationEntryPoint : MonoBehaviour
    {
        [SerializeField] private BootstrapLoadingView _loadingView;

        private InitialSceneBootstrapper _initialSceneBootstrapper;
        private bool _hasStarted;

        [Inject]
        private void Construct(InitialSceneBootstrapper initialSceneBootstrapper)
        {
            _initialSceneBootstrapper = initialSceneBootstrapper;
        }

        private async UniTaskVoid Start()
        {
            if (_hasStarted)
                return;

            if (_initialSceneBootstrapper == null)
            {
                Debug.LogError(
                    "ApplicationEntryPoint was not injected. Ensure the Bootstrap scene contains an active SceneContext.",
                    this);
                enabled = false;
                return;
            }

            _hasStarted = true;
            try
            {
                await _initialSceneBootstrapper.LoadInitialSceneAsync(
                    _loadingView,
                    destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

    }
}
