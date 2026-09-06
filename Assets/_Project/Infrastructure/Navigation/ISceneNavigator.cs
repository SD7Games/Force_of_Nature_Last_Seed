using System.Threading;
using Cysharp.Threading.Tasks;

namespace LastSeed.Infrastructure.Navigation
{
    public interface ISceneNavigator<TScene>
    {
        bool IsNavigating { get; }

        UniTask<bool> TryNavigateAsync(
            TScene scene,
            CancellationToken cancellationToken);

        UniTask<bool> TryNavigateAsync(
            TScene scene,
            ISceneTransition transition,
            CancellationToken cancellationToken);
    }
}
