namespace LastSeed.Infrastructure.Navigation
{
    public static class GameSceneRoutes
    {
        public static SceneRouteCatalog<GameSceneId> CreateCatalog()
        {
            return new SceneRouteCatalog<GameSceneId>(new[]
            {
                new SceneRoute<GameSceneId>(GameSceneId.Bootstrap, GameSceneNames.Bootstrap),
                new SceneRoute<GameSceneId>(GameSceneId.Lobby, GameSceneNames.Lobby),
                new SceneRoute<GameSceneId>(GameSceneId.Gameplay, GameSceneNames.Gameplay)
            });
        }
    }
}
