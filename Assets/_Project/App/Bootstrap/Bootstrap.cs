using System.Collections;
using UnityEngine;

public sealed class Bootstrap
{
    private readonly SceneLoader _sceneLoader;

    public Bootstrap(SceneLoader sceneLoader)
    {
        _sceneLoader = sceneLoader;
    }

    public IEnumerator LoadInitialLobby(BootstrapLoadingView loadingView)
    {
        AsyncOperation loadOperation = _sceneLoader.LoadLobbyAsync(false);

        if (loadingView != null)
            loadingView.Play();

        while (loadingView != null && !loadingView.IsComplete)
            yield return null;

        while (!_sceneLoader.IsReadyToActivate(loadOperation))
            yield return null;

        _sceneLoader.Activate(loadOperation);
    }
}
