using System;
using Cysharp.Threading.Tasks;
using LastSeed.Infrastructure.Navigation;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace _Project.App.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class GameplayBackToLobbyButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private ISceneNavigator<GameSceneId> _sceneNavigator;
        private bool _isReturnRequested;

        [Inject]
        public void Construct(ISceneNavigator<GameSceneId> sceneNavigator)
        {
            _sceneNavigator = sceneNavigator;
        }

        private void Awake()
        {
            if (_button == null)
                TryGetComponent(out _button);
        }

        private void OnEnable()
        {
            _isReturnRequested = false;

            if (_button != null)
                _button.onClick.AddListener(HandleClicked);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_button == null)
                TryGetComponent(out _button);
        }
#endif

        private void HandleClicked()
        {
            if (_isReturnRequested)
                return;

            _isReturnRequested = true;
            SetInteractable(false);

            NavigateAsync().Forget();
        }

        private async UniTask NavigateAsync()
        {
            try
            {
                bool started = await _sceneNavigator.TryNavigateAsync(
                    GameSceneId.Lobby,
                    destroyCancellationToken);

                if (started || this == null)
                    return;

                _isReturnRequested = false;
                SetInteractable(true);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);

                if (this == null)
                    return;

                _isReturnRequested = false;
                SetInteractable(true);
            }
        }

        private void SetInteractable(bool interactable)
        {
            if (_button != null)
                _button.interactable = interactable;
        }
    }
}
