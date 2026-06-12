using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class GameplayBackToLobbyButton : MonoBehaviour
{
    [SerializeField] private Button _button;

    private bool _isReturnRequested;

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

        if (!SceneNavigationEvents.RequestLobby())
        {
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
