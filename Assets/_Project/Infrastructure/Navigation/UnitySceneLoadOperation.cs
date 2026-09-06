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

        public UniTask WaitUntilReadyAsync(CancellationToken cancellationToken)
        {
            return UniTask.WaitUntil(
                _operation,
                static operation => operation.progress >= ReadyForActivationProgress,
                PlayerLoopTiming.Update,
                cancellationToken);
        }

        public void Activate()
        {
            _operation.allowSceneActivation = true;
        }

        public UniTask WaitUntilCompletedAsync(CancellationToken cancellationToken)
        {
            return UniTask.WaitUntil(
                _operation,
                static operation => operation.isDone,
                PlayerLoopTiming.Update,
                cancellationToken);
        }
    }
}
