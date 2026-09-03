using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EntryPoint : MonoBehaviour
{
    [SerializeField] private BootstrapLoadingView _loadingView;

    private static EntryPoint _instance;
    private Bootstrap _bootstrap;
    private SceneLoader _sceneLoader;
    private Coroutine _loadRoutine;
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

        _sceneLoader = new SceneLoader();
        _bootstrap = new Bootstrap(_sceneLoader);
    }

    private void OnEnable()
    {
        if (_instance != this)
            return;

        SceneNavigationEvents.GameLoadRequested += HandleGameLoadRequested;
        SceneNavigationEvents.LobbyLoadRequested += HandleLobbyLoadRequested;
    }

    private void OnDisable()
    {
        if (_instance != this)
            return;

        SceneNavigationEvents.GameLoadRequested -= HandleGameLoadRequested;
        SceneNavigationEvents.LobbyLoadRequested -= HandleLobbyLoadRequested;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Start()
    {
        if (_started)
            return;

        _started = true;
        _loadRoutine = StartCoroutine(LoadInitialLobbyRoutine());
    }

    private void HandleGameLoadRequested()
    {
        LoadScene(SceneNames.Game);
    }

    private void HandleLobbyLoadRequested()
    {
        LoadScene(SceneNames.Lobby);
    }

    private void LoadScene(string sceneName)
    {
        if (_loadRoutine != null)
            return;

        _loadRoutine = StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        Time.timeScale = 1f;

        AsyncOperation operation = _sceneLoader.LoadAsync(sceneName, false);

        while (!_sceneLoader.IsReadyToActivate(operation))
            yield return null;

        _sceneLoader.Activate(operation);
        _loadRoutine = null;
    }

    private IEnumerator LoadInitialLobbyRoutine()
    {
        yield return _bootstrap.LoadInitialLobby(_loadingView);
        _loadRoutine = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        _instance = null;
    }
}
