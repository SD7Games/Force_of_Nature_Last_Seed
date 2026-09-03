using System.Globalization;

public static class WormHpFormatter
{
    private const int Thousand = 1000;
    private const int TenThousand = 10000;
    private const int Million = 1000000;
    private const int TenMillion = 10000000;

    public static string Format(int value)
    {
        int safeValue = UnityEngine.Mathf.Max(0, value);

        if (safeValue < Thousand)
            return safeValue.ToString(CultureInfo.InvariantCulture);

        if (safeValue < TenThousand)
            return FormatSingleDecimal(safeValue / (float)Thousand) + "k";

        if (safeValue < Million)
            return (safeValue / Thousand).ToString(CultureInfo.InvariantCulture) + "k";

        if (safeValue < TenMillion)
            return FormatSingleDecimal(safeValue / (float)Million) + "m";

        return (safeValue / Million).ToString(CultureInfo.InvariantCulture) + "m";
    }

    public static bool TryFormat(int value, char[] buffer, out int length)
    {
        length = 0;

        if (buffer == null || buffer.Length == 0)
            return false;

        int safeValue = UnityEngine.Mathf.Max(0, value);

        if (safeValue < Thousand)
            return TryAppendInt(safeValue, buffer, ref length);

        if (safeValue < TenThousand)
            return TryAppendSingleDecimal(safeValue, Thousand, 'k', buffer, ref length);

        if (safeValue < Million)
        {
            if (!TryAppendInt(safeValue / Thousand, buffer, ref length))
                return false;

            return TryAppendChar('k', buffer, ref length);
        }

        if (safeValue < TenMillion)
            return TryAppendSingleDecimal(safeValue, Million, 'm', buffer, ref length);

        if (!TryAppendInt(safeValue / Million, buffer, ref length))
            return false;

        return TryAppendChar('m', buffer, ref length);
    }

    private static string FormatSingleDecimal(float value)
    {
        float rounded = UnityEngine.Mathf.Round(value * 10f) * 0.1f;
        float capped = UnityEngine.Mathf.Min(9.9f, rounded);

        return capped.ToString("0.#", CultureInfo.InvariantCulture);
    }

    private static bool TryAppendSingleDecimal(
        int value,
        int divisor,
        char suffix,
        char[] buffer,
        ref int length)
    {
        int tenths = UnityEngine.Mathf.RoundToInt(value * 10f / divisor);
        tenths = UnityEngine.Mathf.Min(99, tenths);

        int whole = tenths / 10;
        int fraction = tenths % 10;

        if (!TryAppendInt(whole, buffer, ref length))
            return false;

        if (fraction > 0)
        {
            if (!TryAppendChar('.', buffer, ref length))
                return false;

            if (!TryAppendChar((char)('0' + fraction), buffer, ref length))
                return false;
        }

        return TryAppendChar(suffix, buffer, ref length);
    }

    private static bool TryAppendInt(int value, char[] buffer, ref int length)
    {
        if (value == 0)
            return TryAppendChar('0', buffer, ref length);

        int digitCount = 0;
        int remaining = value;

        while (remaining > 0)
        {
            digitCount++;
            remaining /= 10;
        }

        if (length + digitCount > buffer.Length)
            return false;

        int writeIndex = length + digitCount - 1;
        remaining = value;

        while (remaining > 0)
        {
            buffer[writeIndex] = (char)('0' + (remaining % 10));
            writeIndex--;
            remaining /= 10;
        }

        length += digitCount;
        return true;
    }

    private static bool TryAppendChar(char value, char[] buffer, ref int length)
    {
        if (length >= buffer.Length)
            return false;

        buffer[length] = value;
        length++;
        return true;
    }
}
