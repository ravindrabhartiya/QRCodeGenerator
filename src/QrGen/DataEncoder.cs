using System;

namespace QrGen;

/// <summary>
/// Encodes input text into the QR data bit stream (Step 2): mode indicator, character-count
/// indicator, mode-specific data bits, and padding to the version/EC-level capacity.
/// </summary>
public static class DataEncoder
{
    private const byte PadByteA = 0xEC;
    private const byte PadByteB = 0x11;

    /// <summary>
    /// Builds the unpadded segment (mode indicator + character-count indicator + data bits).
    /// </summary>
    /// <param name="input">The text to encode; must be valid for <paramref name="mode"/>.</param>
    /// <param name="mode">The encoding mode to use.</param>
    public static BitBuffer EncodeSegment(string input, QrMode mode)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));

        var buffer = new BitBuffer();
        buffer.AppendBits(ModeIndicator(mode), 4);

        int countBits = Version4.CharCountBits(mode);
        int count = input.Length;
        if (count >= (1 << countBits))
            throw new ArgumentException(
                $"Character count {count} does not fit in the {countBits}-bit indicator.",
                nameof(input));
        buffer.AppendBits(count, countBits);

        EncodeData(input, mode, buffer);
        return buffer;
    }

    /// <summary>
    /// Applies the terminator, byte alignment, and 0xEC/0x11 pad bytes to fill
    /// <paramref name="capacityBits"/>, returning the packed data codewords.
    /// </summary>
    public static byte[] PadToCapacity(BitBuffer segment, int capacityBits)
    {
        if (segment is null) throw new ArgumentNullException(nameof(segment));
        if (capacityBits <= 0 || capacityBits % 8 != 0)
            throw new ArgumentException(
                "capacityBits must be a positive multiple of 8.", nameof(capacityBits));
        if (segment.Count > capacityBits)
            throw new ArgumentException(
                $"Segment ({segment.Count} bits) exceeds capacity ({capacityBits} bits).",
                nameof(segment));

        var buffer = Clone(segment);

        // Terminator: up to 4 zero bits, but never past capacity.
        int terminator = Math.Min(4, capacityBits - buffer.Count);
        for (int i = 0; i < terminator; i++) buffer.AppendBit(false);

        // Pad with zeros to the next byte boundary.
        while (buffer.Count % 8 != 0) buffer.AppendBit(false);

        // Fill remaining space with alternating pad bytes.
        bool useA = true;
        while (buffer.Count < capacityBits)
        {
            buffer.AppendByte(useA ? PadByteA : PadByteB);
            useA = !useA;
        }

        return buffer.ToBytes();
    }

    /// <summary>Encodes <paramref name="input"/> to full Version-4 data codewords.</summary>
    public static byte[] Encode(string input, QrMode mode, EcLevel ec)
    {
        var segment = EncodeSegment(input, mode);
        int capacityBits = Version4.DataCodewords(ec) * 8;
        if (segment.Count > capacityBits)
            throw new ArgumentException(
                $"Encoded data ({segment.Count} bits) exceeds Version 4 {ec} capacity " +
                $"({capacityBits} bits).",
                nameof(input));
        return PadToCapacity(segment, capacityBits);
    }

    /// <summary>Encodes <paramref name="input"/> using the auto-detected mode.</summary>
    public static byte[] Encode(string input, EcLevel ec)
        => Encode(input, ModeDetector.Detect(input), ec);

    private static int ModeIndicator(QrMode mode) => mode switch
    {
        QrMode.Numeric => 0b0001,
        QrMode.Alphanumeric => 0b0010,
        QrMode.Byte => 0b0100,
        QrMode.Kanji => 0b1000,
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    private static void EncodeData(string input, QrMode mode, BitBuffer buffer)
    {
        switch (mode)
        {
            case QrMode.Numeric: EncodeNumeric(input, buffer); break;
            case QrMode.Alphanumeric: EncodeAlphanumeric(input, buffer); break;
            case QrMode.Byte: EncodeByte(input, buffer); break;
            case QrMode.Kanji: EncodeKanji(input, buffer); break;
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    private static void EncodeNumeric(string s, BitBuffer buffer)
    {
        for (int i = 0; i < s.Length; i += 3)
        {
            int len = Math.Min(3, s.Length - i);
            int value = 0;
            for (int j = 0; j < len; j++)
            {
                char c = s[i + j];
                if (c < '0' || c > '9')
                    throw new ArgumentException($"'{c}' is not a digit.", nameof(s));
                value = value * 10 + (c - '0');
            }
            int bits = len == 3 ? 10 : len == 2 ? 7 : 4;
            buffer.AppendBits(value, bits);
        }
    }

    private static void EncodeAlphanumeric(string s, BitBuffer buffer)
    {
        for (int i = 0; i < s.Length; i += 2)
        {
            if (i + 1 < s.Length)
            {
                int value = 45 * Value(s[i]) + Value(s[i + 1]);
                buffer.AppendBits(value, 11);
            }
            else
            {
                buffer.AppendBits(Value(s[i]), 6);
            }
        }
    }

    private static int Value(char c)
    {
        int v = AlphanumericTable.Value(c);
        if (v < 0)
            throw new ArgumentException($"'{c}' is not an alphanumeric character.");
        return v;
    }

    private static void EncodeByte(string s, BitBuffer buffer)
    {
        foreach (char c in s)
        {
            if (c > 0xFF)
                throw new ArgumentException(
                    $"'{c}' is not representable in ISO-8859-1.", nameof(s));
            buffer.AppendBits(c, 8);
        }
    }

    private static void EncodeKanji(string s, BitBuffer buffer)
    {
        foreach (char c in s)
        {
            byte[] bytes = ShiftJisEncoding.Strict.GetBytes(c.ToString());
            if (bytes.Length != 2)
                throw new ArgumentException($"'{c}' is not a Shift JIS double-byte character.");

            int sjis = (bytes[0] << 8) | bytes[1];
            int t;
            if (sjis >= 0x8140 && sjis <= 0x9FFC) t = sjis - 0x8140;
            else if (sjis >= 0xE040 && sjis <= 0xEBBF) t = sjis - 0xC140;
            else throw new ArgumentException($"'{c}' is outside the QR Kanji range.");

            int value = ((t >> 8) * 0xC0) + (t & 0xFF);
            buffer.AppendBits(value, 13);
        }
    }

    private static BitBuffer Clone(BitBuffer source)
    {
        var copy = new BitBuffer();
        for (int i = 0; i < source.Count; i++) copy.AppendBit(source[i]);
        return copy;
    }
}
