using System;
using System.Collections.Generic;
using System.Text;

namespace QrGen;

/// <summary>
/// An append-only sequence of bits, used to assemble QR data before packing into codewords.
/// </summary>
public sealed class BitBuffer
{
    private readonly List<bool> _bits = new();

    /// <summary>The number of bits currently stored.</summary>
    public int Count => _bits.Count;

    /// <summary>Gets the bit at <paramref name="index"/> (true = 1).</summary>
    public bool this[int index] => _bits[index];

    /// <summary>Appends a single bit.</summary>
    public void AppendBit(bool bit) => _bits.Add(bit);

    /// <summary>
    /// Appends the low <paramref name="bitCount"/> bits of <paramref name="value"/>,
    /// most-significant bit first.
    /// </summary>
    /// <param name="value">The value whose bits are appended; must be non-negative.</param>
    /// <param name="bitCount">Number of bits to append (0-31).</param>
    public void AppendBits(int value, int bitCount)
    {
        if (bitCount < 0 || bitCount > 31)
            throw new ArgumentOutOfRangeException(nameof(bitCount), "bitCount must be 0-31.");
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative.");
        if (bitCount < 31 && value >= (1 << bitCount))
            throw new ArgumentOutOfRangeException(
                nameof(value), $"value {value} does not fit in {bitCount} bits.");

        for (int i = bitCount - 1; i >= 0; i--)
            _bits.Add(((value >> i) & 1) == 1);
    }

    /// <summary>Appends a full byte (8 bits, MSB first).</summary>
    public void AppendByte(byte value) => AppendBits(value, 8);

    /// <summary>Renders the buffer as a string of '0' and '1' characters.</summary>
    public string ToBitString()
    {
        var sb = new StringBuilder(_bits.Count);
        foreach (bool b in _bits) sb.Append(b ? '1' : '0');
        return sb.ToString();
    }

    /// <summary>
    /// Packs the bits into bytes (MSB first). Requires the length to be a multiple of 8.
    /// </summary>
    public byte[] ToBytes()
    {
        if (_bits.Count % 8 != 0)
            throw new InvalidOperationException(
                $"Bit count {_bits.Count} is not byte-aligned.");

        var bytes = new byte[_bits.Count / 8];
        for (int i = 0; i < _bits.Count; i++)
            if (_bits[i])
                bytes[i / 8] |= (byte)(1 << (7 - (i % 8)));
        return bytes;
    }
}
