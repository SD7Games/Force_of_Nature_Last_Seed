using LastSeed.Infrastructure.Navigation;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class LobbyStartBattleButton : MonoBehaviour
{
    [SerializeField] private Button _button;

    private ISceneNavigationService _sceneNavigationService;
    private bool _isLoadingRequested;

    [Inject]
    public void Construct(ISceneNavigationService sceneNavigationService)
    {
        _sceneNavigationService = sceneNavigationService;
    }

    private void Awake()
    {
        if (_button == null)
            TryGetComponent(out _button);
    }

    private void OnEnable()
    {
        _isLoadingRequested = false;

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
        if (_isLoadingRequested)
            return;

        _isLoadingRequested = true;
        SetInteractable(false);

        if (!_sceneNavigationService.TryLoadGameplayScene())
        {
            _isLoadingRequested = false;
            SetInteractable(true);
        }
    }

    private void SetInteractable(bool interactable)
    {
        if (_button != null)
            _button.interactable = interactable;
    }
}
