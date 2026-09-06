using NUnit.Framework;
using UnityEngine;

namespace LastSeed.Tests
{
    public sealed class WormSegmentPooledViewLifecycleTests
    {
        private GameObject _owner;
        private GameObject _visual;
        private BoxCollider2D _collider;
        private WormSegmentPooledViewLifecycle _lifecycle;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("Segment");
            _visual = new GameObject("Visual");
            _visual.transform.SetParent(_owner.transform, false);
            _collider = _owner.AddComponent<BoxCollider2D>();
            _lifecycle = new WormSegmentPooledViewLifecycle(
                _owner,
                _visual.transform,
                _collider);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
        }

        [Test]
        public void Kill_DisablesCompleteRuntimeView()
        {
            _lifecycle.Kill();

            Assert.That(_lifecycle.IsAlive, Is.False);
            Assert.That(_owner.activeSelf, Is.False);
            Assert.That(_visual.activeSelf, Is.False);
            Assert.That(_collider.enabled, Is.False);
        }

        [Test]
        public void KilledView_IgnoresRuntimeVisibilityUntilReactivated()
        {
            _lifecycle.Kill();

            _lifecycle.SetRuntimeVisible(true);

            Assert.That(_owner.activeSelf, Is.False);

            _lifecycle.Activate();

            Assert.That(_lifecycle.IsAlive, Is.True);
            Assert.That(_owner.activeSelf, Is.True);
            Assert.That(_visual.activeSelf, Is.True);
            Assert.That(_collider.enabled, Is.True);
        }

        [Test]
        public void PrepareForPool_RestoresReusableStateBeforeDeactivation()
        {
            _lifecycle.Kill();

            _lifecycle.PrepareForPool();

            Assert.That(_lifecycle.IsAlive, Is.True);
            Assert.That(_owner.activeSelf, Is.False);
            Assert.That(_visual.activeSelf, Is.True);
            Assert.That(_collider.enabled, Is.True);
        }
    }
}
