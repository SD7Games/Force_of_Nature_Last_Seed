using NUnit.Framework;
using UnityEngine;

namespace LastSeed.Tests
{
    public sealed class RewardTextFormatterTests
    {
        private static readonly Color32 Red = new(255, 0, 0, 255);

        [Test]
        public void HighlightNumbers_FormatsSignedDecimalAndMultiplierRanges()
        {
            RewardTextFormatter formatter = new();

            string result = formatter.HighlightNumbers("Gain +12.5% and x3", Red);

            Assert.That(
                result,
                Is.EqualTo(
                    "Gain <color=#FF0000>+12.5%</color> and <color=#FF0000>x3</color>"));
        }

        [Test]
        public void HighlightNumbers_ReusesInstanceWithoutLeakingPreviousRanges()
        {
            RewardTextFormatter formatter = new();
            formatter.HighlightNumbers("Value 10", Red);

            string result = formatter.HighlightNumbers("No values", Red);

            Assert.That(result, Is.EqualTo("No values"));
        }

        [Test]
        public void FormatRarityLine_InsertsColoredRarity()
        {
            RewardTextFormatter formatter = new();

            string result = formatter.FormatRarityLine(
                "Guaranteed: {0}",
                RewardRarity.Legendary,
                new Color32(255, 255, 255, 255),
                new Color32(0, 0, 255, 255),
                Red);

            Assert.That(
                result,
                Is.EqualTo("Guaranteed: <color=#FF0000>Legendary</color>"));
        }
    }
}
