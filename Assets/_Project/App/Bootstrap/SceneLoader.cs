using UnityEngine.SceneManagement;

public static class SceneNames
{
    public const string Game = "Game";
}

public sealed class SceneLoader
{
    public void LoadGame()
    {
        SceneManager.LoadScene(SceneNames.Game);
    }
}