using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class RewardTextFormatter
{
    private readonly List<HighlightRange> _ranges = new(8);
    private readonly StringBuilder _builder = new(128);

    public string HighlightAttempts(string text, Color32 numberColor)
    {
        return HighlightNumbers(text, numberColor);
    }

    public string HighlightNumbers(string text, Color32 numberColor)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        _ranges.Clear();
        AddNumberRanges(text, ToHex(numberColor));
        return BuildHighlightedText(text);
    }

    public string FormatRarityLine(
        string format,
        RewardRarity rarity,
        Color32 commonColor,
        Color32 rareColor,
        Color32 legendaryColor)
    {
        string rarityText = GetRarityText(rarity);
        string color = ToHex(GetRarityColor(rarity, commonColor, rareColor, legendaryColor));
        return string.Format(format, $"<color=#{color}>{rarityText}</color>");
    }

    public string FormatRarityLine(
        string format,
        RewardRarity rarity,
        Color32 commonColor,
        Color32 rareColor,
        Color32 legendaryColor,
        Color32 numberColor)
    {
        const string rarityToken = "__RARITY__";

        string rarityText = GetRarityText(rarity);
        string rarityColor = ToHex(GetRarityColor(rarity, commonColor, rareColor, legendaryColor));
        string highlightedFormat = HighlightNumbers(format.Replace("{0}", rarityToken), numberColor);
        return highlightedFormat.Replace(
            rarityToken,
            $"<color=#{rarityColor}>{rarityText}</color>");
    }

    private void AddNumberRanges(string text, string colorHex)
    {
        for (int index = 0; index < text.Length; index++)
        {
            if (!IsNumberStart(text, index))
                continue;

            int start = index;
            bool hasDigit = false;

            if (text[index] == '+' || text[index] == '-' || text[index] == 'x' || text[index] == 'X')
                index++;

            for (; index < text.Length && IsNumberBody(text[index]); index++)
            {
                if (char.IsDigit(text[index]))
                    hasDigit = true;
            }

            if (hasDigit)
                TryAddRange(start, index - start, colorHex);

            index--;
        }
    }

    private static bool IsNumberStart(string text, int index)
    {
        char c = text[index];

        if (char.IsDigit(c))
            return true;

        if ((c == '+' || c == '-' || c == 'x' || c == 'X')
            && index + 1 < text.Length
            && char.IsDigit(text[index + 1]))
        {
            return true;
        }

        return false;
    }

    private static bool IsNumberBody(char c)
    {
        return char.IsDigit(c)
            || c == '.'
            || c == ','
            || c == '/'
            || c == '%';
    }

    private bool TryAddRange(int start, int length, string colorHex)
    {
        if (length <= 0)
            return false;

        int end = start + length;

        for (int i = 0; i < _ranges.Count; i++)
        {
            HighlightRange range = _ranges[i];
            int rangeEnd = range.Start + range.Length;

            if (start < rangeEnd && end > range.Start)
                return false;
        }

        _ranges.Add(new HighlightRange(start, length, colorHex));
        return true;
    }

    private string BuildHighlightedText(string text)
    {
        if (_ranges.Count == 0)
            return text;

        _ranges.Sort(CompareRanges);
        _builder.Clear();

        int cursor = 0;

        for (int i = 0; i < _ranges.Count; i++)
        {
            HighlightRange range = _ranges[i];

            if (range.Start > cursor)
                _builder.Append(text, cursor, range.Start - cursor);

            _builder.Append("<color=#");
            _builder.Append(range.ColorHex);
            _builder.Append(">");
            _builder.Append(text, range.Start, range.Length);
            _builder.Append("</color>");

            cursor = range.Start + range.Length;
        }

        if (cursor < text.Length)
            _builder.Append(text, cursor, text.Length - cursor);

        return _builder.ToString();
    }

    private static int CompareRanges(HighlightRange left, HighlightRange right)
    {
        return left.Start.CompareTo(right.Start);
    }

    private static string GetRarityText(RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.Rare:
                return "Rare";
            case RewardRarity.Legendary:
                return "Legendary";
            default:
                return "Common";
        }
    }

    private static Color32 GetRarityColor(
        RewardRarity rarity,
        Color32 commonColor,
        Color32 rareColor,
        Color32 legendaryColor)
    {
        switch (rarity)
        {
            case RewardRarity.Rare:
                return rareColor;
            case RewardRarity.Legendary:
                return legendaryColor;
            default:
                return commonColor;
        }
    }

    private static string ToHex(Color32 color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }

    private readonly struct HighlightRange
    {
        public HighlightRange(int start, int length, string colorHex)
        {
            Start = start;
            Length = length;
            ColorHex = colorHex;
        }

        public int Start { get; }
        public int Length { get; }
        public string ColorHex { get; }
    }
}
