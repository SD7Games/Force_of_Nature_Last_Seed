using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNavigationEvents
{
    public static event Action GameLoadRequested;
    public static event Action LobbyLoadRequested;

    public static bool RequestGame()
    {
        return Invoke(GameLoadRequested, "Game");
    }

    public static bool RequestLobby()
    {
        return Invoke(LobbyLoadRequested, "Lobby");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        GameLoadRequested = null;
        LobbyLoadRequested = null;
    }

    private static bool Invoke(Action request, string sceneName)
    {
        if (request != null)
        {
            request.Invoke();
            return true;
        }

        return TryLoadSceneDirectly(sceneName);
    }

    private static bool TryLoadSceneDirectly(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneNavigationEvents: scene name is empty.");
            return false;
        }

        Time.timeScale = 1f;
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation != null)
            return true;

        Debug.LogError($"SceneNavigationEvents: failed to load '{sceneName}' scene directly because no listener is registered.");
        return false;
    }
}
