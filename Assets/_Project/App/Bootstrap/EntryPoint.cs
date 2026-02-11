using UnityEngine;

public sealed class EntryPoint : MonoBehaviour
{
    private static EntryPoint _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartGame();
    }

    private void StartGame()
    {
        var sceneLoader = new SceneLoader();
        var gameBootstrap = new Bootstrap(sceneLoader);
        gameBootstrap.StartGame();
    }
}