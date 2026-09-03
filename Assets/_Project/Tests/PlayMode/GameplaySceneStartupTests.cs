using System;
using System.Collections;
using LastSeed.Bootstrap.GameplayLoop;
using LastSeed.Gameplay.Combat;
using LastSeed.Gameplay.Signals;
using LastSeed.Infrastructure.Input;
using LastSeed.Infrastructure.Navigation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;

namespace LastSeed.Tests.PlayMode
{
    public sealed class GameplaySceneStartupTests
    {
        private const int InitializationFrameCount = 2;

        [UnityTest]
        public IEnumerator GameplayScene_WhenLoadedDirectly_ResolvesRequiredRuntimeDependencies()
        {
            AsyncOperation sceneLoadOperation = SceneManager.LoadSceneAsync(
                GameSceneNames.Gameplay,
                LoadSceneMode.Single);

            Assert.That(sceneLoadOperation, Is.Not.Null);
            yield return sceneLoadOperation;

            for (int frameIndex = 0; frameIndex < InitializationFrameCount; frameIndex++)
                yield return null;

            Scene gameplayScene = SceneManager.GetActiveScene();
            SceneContext sceneContext = FindInScene<SceneContext>(gameplayScene);

            Assert.That(gameplayScene.name, Is.EqualTo(GameSceneNames.Gameplay));
            Assert.That(ProjectContext.HasInstance, Is.True);
            Assert.That(sceneContext, Is.Not.Null);
            Assert.That(FindInScene<PlayerInputSnapshotProvider>(gameplayScene), Is.Not.Null);
            Assert.That(FindInScene<GameplayUpdateDriver>(gameplayScene), Is.Not.Null);

            AssertPlayerServices(sceneContext.Container);
            AssertWormServices(sceneContext.Container);
            AssertCombatSessionSignals(sceneContext.Container);
            AssertWormBurstSignal(sceneContext.Container);
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertPlayerServices(DiContainer sceneContainer)
        {
            Assert.That(sceneContainer.Resolve<PlayerMovementController>(), Is.Not.Null);
            Assert.That(sceneContainer.Resolve<PlayerWeaponController>(), Is.Not.Null);
            Assert.That(sceneContainer.Resolve<ProjectileWeapon>(), Is.Not.Null);
        }

        private static void AssertWormServices(DiContainer sceneContainer)
        {
            Assert.That(sceneContainer.Resolve<WormAdaptiveHpController>(), Is.Not.Null);
            Assert.That(sceneContainer.Resolve<WormCombatBurstController>(), Is.Not.Null);
            Assert.That(sceneContainer.Resolve<WormRailTargetResolver>(), Is.Not.Null);
            Assert.That(sceneContainer.Resolve<WormSegmentChainPresenter>(), Is.Not.Null);
            Assert.That(sceneContainer.Resolve<WormReviveMotionCalculator>(), Is.Not.Null);
            Assert.That(sceneContainer.Resolve<WormReviveVisualScaler>(), Is.Not.Null);
            Assert.That(
                sceneContainer.Resolve<WormSectionRollbackState<WormSegment>>(),
                Is.Not.Null);
            Assert.That(sceneContainer.Resolve<WormSpawnSettings>(), Is.Not.Null);
            Assert.That(sceneContainer.Resolve<WormSegmentPool>(), Is.Not.Null);
            Assert.That(sceneContainer.Resolve<WormFactory>(), Is.Not.Null);
            Assert.That(sceneContainer.Resolve<WormCombatBurstSignalPublisher>(), Is.Not.Null);
            Assert.That(sceneContainer.Resolve<WormPathCompletedSignalPublisher>(), Is.Not.Null);
            Assert.That(sceneContainer.Resolve<WormFaceBurstPresenter>(), Is.Not.Null);
        }

        private static void AssertWormBurstSignal(DiContainer sceneContainer)
        {
            SignalBus signalBus = sceneContainer.Resolve<SignalBus>();
            WormCombatBurstController burstController =
                sceneContainer.Resolve<WormCombatBurstController>();
            bool receivedActiveState = false;
            Action<WormCombatBurstStateChangedSignal> handler = signal =>
                receivedActiveState = signal.IsActive;
            WormCombatBurstSettings settings = new(
                enabled: true,
                burstSpeed: 3f,
                interval: 0.1f,
                duration: 1f,
                slowdownDuration: 0.2f);

            signalBus.Subscribe(handler);
            burstController.Reset(baseSpeed: 1f);
            burstController.ResolveForwardSpeed(0.1f, 1f, 1f, false, true, settings);
            burstController.ResolveForwardSpeed(0.1f, 1f, 1f, false, true, settings);

            Assert.That(receivedActiveState, Is.True);

            signalBus.Unsubscribe(handler);
            burstController.Reset(baseSpeed: 1f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (ProjectContext.HasInstance)
                UnityEngine.Object.Destroy(ProjectContext.Instance.gameObject);

            yield return null;
        }

        private static TComponent FindInScene<TComponent>(Scene scene)
            where TComponent : Component
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();

            for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
            {
                TComponent component = rootObjects[rootIndex].GetComponentInChildren<TComponent>(true);

                if (component != null)
                    return component;
            }

            return null;
        }

        private static void AssertCombatSessionSignals(DiContainer sceneContainer)
        {
            ICombatSessionState combatSessionState = sceneContainer.Resolve<ICombatSessionState>();
            SignalBus signalBus = sceneContainer.Resolve<SignalBus>();
            bool receivedShootingEnabledSignal = false;
            Action<CombatShootingStateChangedSignal> signalHandler = signal =>
                receivedShootingEnabledSignal = signal.IsShootingEnabled;

            signalBus.Subscribe(signalHandler);
            combatSessionState.SetShootingEnabled(true);

            Assert.That(combatSessionState.IsShootingEnabled, Is.True);
            Assert.That(receivedShootingEnabledSignal, Is.True);

            signalBus.Unsubscribe(signalHandler);
            combatSessionState.Reset();
        }
    }
}
