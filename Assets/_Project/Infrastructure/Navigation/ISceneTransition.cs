using System.Threading;
using Cysharp.Threading.Tasks;

namespace LastSeed.Infrastructure.Navigation
{
    public interface ISceneTransition
    {
        UniTask PlayAsync(CancellationToken cancellationToken);
    }
}
