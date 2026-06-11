using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNames
{
    public const string Lobby = "Lobby";
    public const string Game = "Game";
}

public sealed class SceneLoader
{
    public AsyncOperation LoadLobbyAsync(bool allowSceneActivation)
    {
        return LoadAsync(SceneNames.Lobby, allowSceneActivation);
    }

    public AsyncOperation LoadGameAsync(bool allowSceneActivation)
    {
        return LoadAsync(SceneNames.Game, allowSceneActivation);
    }

    public AsyncOperation LoadAsync(string sceneName, bool allowSceneActivation)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneLoader: scene name is empty.");
            return null;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation != null)
            operation.allowSceneActivation = allowSceneActivation;
        else
            Debug.LogError($"SceneLoader: failed to start loading scene '{sceneName}'.");

        return operation;
    }

    public void Activate(AsyncOperation operation)
    {
        if (operation != null)
            operation.allowSceneActivation = true;
    }

    public bool IsReadyToActivate(AsyncOperation operation)
    {
        return operation == null || operation.progress >= 0.9f;
    }
}
