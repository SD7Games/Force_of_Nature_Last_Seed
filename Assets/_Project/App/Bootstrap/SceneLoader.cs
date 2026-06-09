using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNames
{
    public const string Game = "Game";
}

public sealed class SceneLoader
{
    public AsyncOperation LoadGameAsync(bool allowSceneActivation)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneNames.Game);

        if (operation != null)
            operation.allowSceneActivation = allowSceneActivation;

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
