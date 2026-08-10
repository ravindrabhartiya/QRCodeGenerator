using System;
using System.Text;

namespace QrGen;

/// <summary>Determines the simplest QR encoding mode that can represent a given input.</summary>
public static class ModeDetector
{
    /// <summary>
    /// Returns the simplest <see cref="QrMode"/> capable of encoding <paramref name="input"/>,
    /// checked in order Numeric → Alphanumeric → Byte → Kanji.
    /// </summary>
    /// <param name="input">The text to encode; must be non-null and non-empty.</param>
    /// <returns>The most compact sufficient mode.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="input"/> is empty or cannot be encoded by any supported mode.
    /// </exception>
    public static QrMode Detect(string input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (input.Length == 0)
            throw new ArgumentException("Input must not be empty.", nameof(input));

        if (IsNumeric(input)) return QrMode.Numeric;
        if (IsAlphanumeric(input)) return QrMode.Alphanumeric;
        if (IsByte(input)) return QrMode.Byte;
        if (IsKanji(input)) return QrMode.Kanji;

        throw new ArgumentException(
            "Input cannot be encoded by any supported QR mode " +
            "(numeric, alphanumeric, byte, kanji).",
            nameof(input));
    }

    private static bool IsNumeric(string s)
    {
        foreach (char c in s)
            if (c < '0' || c > '9') return false;
        return true;
    }

    private static bool IsAlphanumeric(string s)
    {
        foreach (char c in s)
            if (!AlphanumericTable.Contains(c)) return false;
        return true;
    }

    private static bool IsByte(string s)
    {
        // ISO-8859-1 maps exactly to code points 0x00-0xFF.
        foreach (char c in s)
            if (c > 0xFF) return false;
        return true;
    }

    private static bool IsKanji(string s)
    {
        foreach (char c in s)
        {
            if (!TryGetShiftJisDoubleByte(c, out int value)) return false;
            bool inRange =
                (value >= 0x8140 && value <= 0x9FFC) ||
                (value >= 0xE040 && value <= 0xEBBF);
            if (!inRange) return false;
        }
        return true;
    }

    private static bool TryGetShiftJisDoubleByte(char c, out int value)
    {
        value = 0;
        if (char.IsSurrogate(c)) return false;

        byte[] bytes;
        try
        {
            bytes = ShiftJisEncoding.Strict.GetBytes(c.ToString());
        }
        catch (EncoderFallbackException)
        {
            return false;
        }

        if (bytes.Length != 2) return false;
        value = (bytes[0] << 8) | bytes[1];
        return true;
    }
}
