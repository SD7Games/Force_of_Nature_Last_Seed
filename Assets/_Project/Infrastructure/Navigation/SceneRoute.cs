using System;

namespace LastSeed.Infrastructure.Navigation
{
    public readonly struct SceneRoute<TScene>
    {
        public SceneRoute(TScene key, string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new ArgumentException("Scene name cannot be empty.", nameof(sceneName));

            Key = key;
            SceneName = sceneName;
        }

        public TScene Key { get; }
        public string SceneName { get; }
    }
}
