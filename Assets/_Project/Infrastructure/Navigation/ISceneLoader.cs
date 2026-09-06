namespace LastSeed.Infrastructure.Navigation
{
    public interface ISceneLoader
    {
        ISceneLoadOperation BeginLoad(string sceneName);
    }
}
