using UnityEngine;

public sealed class DevScreenInitializer : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_camera == null)
        {
            Debug.LogError("DevScreenInitializer: Camera not found.");
        }

        ScreenBounds.Calculate(_camera);
    }
}