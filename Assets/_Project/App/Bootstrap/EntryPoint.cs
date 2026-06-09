using UnityEngine;

[DisallowMultipleComponent]
public sealed class EntryPoint : MonoBehaviour
{
    [SerializeField] private BootstrapLoadingView _loadingView;

    private static EntryPoint _instance;
    private Bootstrap _bootstrap;
    private bool _started;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _bootstrap = new Bootstrap(new SceneLoader());
    }

    private void Start()
    {
        if (_started)
            return;

        _started = true;
        StartCoroutine(_bootstrap.StartGame(_loadingView));
    }
}
