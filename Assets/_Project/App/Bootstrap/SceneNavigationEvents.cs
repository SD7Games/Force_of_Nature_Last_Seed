using System;
using UnityEngine;

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
        if (request == null)
        {
            Debug.LogWarning($"SceneNavigationEvents: '{sceneName}' scene requested but no listener is registered.");
            return false;
        }

        request.Invoke();
        return true;
    }
}
