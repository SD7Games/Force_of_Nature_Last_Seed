using NUnit.Framework;
using UnityEngine;

namespace LastSeed.Tests
{
    public sealed class WormSegmentDamageBindingTests
    {
        private GameObject _segmentObject;
        private GameObject _combatObject;
        private WormSegment _segment;
        private WormCombatController _combat;

        [SetUp]
        public void SetUp()
        {
            _segmentObject = new GameObject("Segment");
            _segment = _segmentObject.AddComponent<WormSegment>();
            _combatObject = new GameObject("Combat");
            _combat = _combatObject.AddComponent<WormCombatController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_segmentObject);
            Object.DestroyImmediate(_combatObject);
        }

        [Test]
        public void Bind_AddsAndInitializesRequiredRootReceiver()
        {
            WormSegmentDamageBinding binding = new(_segmentObject, _segment);

            binding.Bind(_combat);

            Assert.That(binding.ReceiverCount, Is.EqualTo(1));
            Assert.That(
                _segmentObject.TryGetComponent(out WormSegmentDamageReceiver receiver),
                Is.True);
            Assert.That(receiver.GetSegment(), Is.SameAs(_segment));
        }

        [Test]
        public void Bind_InitializesRootAndChildReceiversOnceDiscovered()
        {
            GameObject childObject = new("ChildReceiver");
            childObject.transform.SetParent(_segmentObject.transform, false);
            WormSegmentDamageReceiver childReceiver =
                childObject.AddComponent<WormSegmentDamageReceiver>();
            WormSegmentDamageBinding binding = new(_segmentObject, _segment);

            binding.Bind(_combat);

            Assert.That(binding.ReceiverCount, Is.EqualTo(2));
            Assert.That(childReceiver.GetSegment(), Is.SameAs(_segment));
        }
    }
}
