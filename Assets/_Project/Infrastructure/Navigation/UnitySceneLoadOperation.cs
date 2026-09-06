using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LastSeed.Infrastructure.Navigation
{
    public sealed class UnitySceneLoadOperation : ISceneLoadOperation
    {
        private const float ReadyForActivationProgress = 0.9f;

        private readonly AsyncOperation _operation;

        public UnitySceneLoadOperation(AsyncOperation operation)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
        }

        public async UniTask WaitUntilReadyAsync(CancellationToken cancellationToken)
        {
            while (_operation.progress < ReadyForActivationProgress)
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        public void Activate()
        {
            _operation.allowSceneActivation = true;
        }

        public async UniTask WaitUntilCompletedAsync(CancellationToken cancellationToken)
        {
            while (!_operation.isDone)
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }
}
