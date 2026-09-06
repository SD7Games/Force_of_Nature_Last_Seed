using System.Threading;
using Cysharp.Threading.Tasks;

namespace LastSeed.Infrastructure.Navigation
{
    public interface ISceneLoadOperation
    {
        UniTask WaitUntilReadyAsync(CancellationToken cancellationToken);
        void Activate();
        UniTask WaitUntilCompletedAsync(CancellationToken cancellationToken);
    }
}
