using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LastSeed.Infrastructure.Navigation;
using NUnit.Framework;

namespace LastSeed.Tests
{
    public sealed class SceneNavigatorTests
    {
        [Test]
        public async Task TryNavigateAsync_WaitsForLoadAndTransitionBeforeActivation()
        {
            FakeSceneLoader loader = new();
            FakeSceneTransition transition = new();
            SceneNavigator<TestScene> navigator = CreateNavigator(loader);

            UniTask<bool> navigation = navigator.TryNavigateAsync(
                TestScene.Gameplay,
                transition,
                CancellationToken.None);

            Assert.That(navigator.IsNavigating, Is.True);
            Assert.That(loader.RequestedSceneName, Is.EqualTo("Game"));

            loader.Operation.MarkReady();
            Assert.That(loader.Operation.ActivationCount, Is.Zero);

            transition.Complete();
            bool completed = await navigation;

            Assert.That(completed, Is.True);
            Assert.That(loader.Operation.ActivationCount, Is.EqualTo(1));
            Assert.That(navigator.IsNavigating, Is.False);
        }

        [Test]
        public async Task TryNavigateAsync_RejectsConcurrentNavigation()
        {
            FakeSceneLoader loader = new();
            SceneNavigator<TestScene> navigator = CreateNavigator(loader);

            UniTask<bool> firstNavigation = navigator.TryNavigateAsync(
                TestScene.Lobby,
                CancellationToken.None);

            bool secondNavigation = await navigator.TryNavigateAsync(
                TestScene.Gameplay,
                CancellationToken.None);

            Assert.That(secondNavigation, Is.False);
            Assert.That(loader.BeginLoadCount, Is.EqualTo(1));

            loader.Operation.MarkReady();
            Assert.That(await firstNavigation, Is.True);
        }

        [Test]
        public void TryNavigateAsync_RejectsUnknownRouteBeforeLoading()
        {
            FakeSceneLoader loader = new();
            SceneNavigator<TestScene> navigator = CreateNavigator(loader);

            Assert.ThrowsAsync<System.Collections.Generic.KeyNotFoundException>(async () =>
                await navigator.TryNavigateAsync(
                    (TestScene)99,
                    CancellationToken.None));

            Assert.That(loader.BeginLoadCount, Is.Zero);
        }

        [Test]
        public void TryNavigateAsync_CancellationCompletesOwnedLoadAndReleasesNavigator()
        {
            FakeSceneLoader loader = new();
            SceneNavigator<TestScene> navigator = CreateNavigator(loader);
            using CancellationTokenSource cancellationSource = new();

            UniTask<bool> navigation = navigator.TryNavigateAsync(
                TestScene.Lobby,
                cancellationSource.Token);

            cancellationSource.Cancel();

            Assert.CatchAsync<OperationCanceledException>(async () =>
                await navigation);
            Assert.That(loader.Operation.ActivationCount, Is.EqualTo(1));
            Assert.That(navigator.IsNavigating, Is.False);
        }

        private static SceneNavigator<TestScene> CreateNavigator(FakeSceneLoader loader)
        {
            SceneRouteCatalog<TestScene> catalog = new(new[]
            {
                new SceneRoute<TestScene>(TestScene.Lobby, "Lobby"),
                new SceneRoute<TestScene>(TestScene.Gameplay, "Game")
            });

            return new SceneNavigator<TestScene>(loader, catalog);
        }

        private enum TestScene
        {
            Lobby,
            Gameplay
        }

        private sealed class FakeSceneLoader : ISceneLoader
        {
            public FakeSceneLoadOperation Operation { get; } = new();
            public int BeginLoadCount { get; private set; }
            public string RequestedSceneName { get; private set; }

            public ISceneLoadOperation BeginLoad(string sceneName)
            {
                BeginLoadCount++;
                RequestedSceneName = sceneName;
                return Operation;
            }
        }

        private sealed class FakeSceneLoadOperation : ISceneLoadOperation
        {
            private readonly UniTaskCompletionSource _readiness = new();
            private readonly UniTaskCompletionSource _completion = new();

            public int ActivationCount { get; private set; }

            public UniTask WaitUntilReadyAsync(CancellationToken cancellationToken)
            {
                return _readiness.Task.AttachExternalCancellation(cancellationToken);
            }

            public void Activate()
            {
                ActivationCount++;
                _completion.TrySetResult();
            }

            public UniTask WaitUntilCompletedAsync(CancellationToken cancellationToken)
            {
                return _completion.Task.AttachExternalCancellation(cancellationToken);
            }

            public void MarkReady()
            {
                _readiness.TrySetResult();
            }
        }

        private sealed class FakeSceneTransition : ISceneTransition
        {
            private readonly UniTaskCompletionSource _completion = new();

            public UniTask PlayAsync(CancellationToken cancellationToken)
            {
                return _completion.Task.AttachExternalCancellation(cancellationToken);
            }

            public void Complete()
            {
                _completion.TrySetResult();
            }
        }
    }
}
