using System;
using System.Collections.Generic;

namespace LastSeed.Infrastructure.Navigation
{
    public sealed class SceneRouteCatalog<TScene>
    {
        private readonly Dictionary<TScene, string> _sceneNames;

        public SceneRouteCatalog(
            IEnumerable<SceneRoute<TScene>> routes,
            IEqualityComparer<TScene> comparer = null)
        {
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));

            _sceneNames = new Dictionary<TScene, string>(comparer);

            foreach (SceneRoute<TScene> route in routes)
            {
                if (!_sceneNames.TryAdd(route.Key, route.SceneName))
                    throw new ArgumentException(
                        $"Duplicate scene route key '{route.Key}'.",
                        nameof(routes));
            }
        }

        public int Count => _sceneNames.Count;

        public string GetSceneName(TScene scene)
        {
            if (_sceneNames.TryGetValue(scene, out string sceneName))
                return sceneName;

            throw new KeyNotFoundException(
                $"Scene route '{scene}' is not registered.");
        }
    }
}
