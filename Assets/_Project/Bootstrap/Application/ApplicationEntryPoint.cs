using System.Collections;
using UnityEngine;
using Zenject;

namespace LastSeed.Bootstrap.Application
{
    [DisallowMultipleComponent]
    public sealed class ApplicationEntryPoint : MonoBehaviour
    {
        [SerializeField] private BootstrapLoadingView _loadingView;

        private InitialSceneBootstrapper _initialSceneBootstrapper;
        private Coroutine _initialSceneLoadRoutine;
        private bool _hasStarted;

        [Inject]
        private void Construct(InitialSceneBootstrapper initialSceneBootstrapper)
        {
            _initialSceneBootstrapper = initialSceneBootstrapper;
        }

        private void Start()
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
            _initialSceneLoadRoutine = StartCoroutine(LoadInitialLobbyRoutine());
        }

        private void OnDestroy()
        {
            _initialSceneLoadRoutine = null;
        }

        private IEnumerator LoadInitialLobbyRoutine()
        {
            yield return _initialSceneBootstrapper.LoadInitialLobby(_loadingView);
            _initialSceneLoadRoutine = null;
        }
    }
}
