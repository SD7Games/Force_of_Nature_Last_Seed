namespace LastSeed.Infrastructure.Navigation
{
    public interface ISceneNavigationService
    {
        bool IsLoading { get; }
        bool TryLoadLobbyScene();
        bool TryLoadGameplayScene();
    }
}
