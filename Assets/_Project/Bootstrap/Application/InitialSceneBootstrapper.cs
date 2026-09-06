using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LastSeed.Infrastructure.Navigation;

namespace LastSeed.Bootstrap.Application
{
    public sealed class InitialSceneBootstrapper
    {
        private readonly ISceneNavigator<GameSceneId> _sceneNavigator;

        public InitialSceneBootstrapper(ISceneNavigator<GameSceneId> sceneNavigator)
        {
            _sceneNavigator = sceneNavigator ??
                throw new ArgumentNullException(nameof(sceneNavigator));
        }

        public UniTask<bool> LoadInitialSceneAsync(
            ISceneTransition transition,
            CancellationToken cancellationToken)
        {
            return _sceneNavigator.TryNavigateAsync(
                GameSceneId.Lobby,
                transition,
                cancellationToken);
        }
    }
}
