using System.Collections.Generic;

namespace LastSeed.Tests
{
    internal sealed class TestRandomSource : IRandomSource
    {
        private readonly Queue<float> _values = new();
        private readonly float _fallback;

        public TestRandomSource(float fallback = 0f, params float[] values)
        {
            _fallback = fallback;

            if (values == null)
                return;

            for (int index = 0; index < values.Length; index++)
                _values.Enqueue(values[index]);
        }

        public int Calls { get; private set; }

        public float NextUnitFloat()
        {
            Calls++;
            return _values.Count > 0 ? _values.Dequeue() : _fallback;
        }
    }
}
