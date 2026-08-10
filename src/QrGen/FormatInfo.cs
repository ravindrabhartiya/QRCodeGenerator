using System;

namespace QrGen;

/// <summary>
/// Format-information encoding and placement (§7.9): a 5-bit value (EC level + mask pattern)
/// protected by a BCH(15,5) code and XOR-masked, placed in the two standard copies around the
/// finder patterns.
/// </summary>
public static class FormatInfo
{
    private const int Generator = 0x537;
    private const int FormatMask = 0x5412;

    /// <summary>The two-bit format indicator for an EC level (L=01, M=00, Q=11, H=10).</summary>
    public static int FormatBits(EcLevel ec) => ec switch
    {
        EcLevel.L => 0b01,
        EcLevel.M => 0b00,
        EcLevel.Q => 0b11,
        EcLevel.H => 0b10,
        _ => throw new ArgumentOutOfRangeException(nameof(ec)),
    };

    /// <summary>
    /// Computes the 15-bit format-information value for the given EC level and mask pattern.
    /// </summary>
    public static int Compute(EcLevel ec, int maskPattern)
    {
        if (maskPattern is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(maskPattern), "Mask pattern must be 0–7.");
        }

        int data = (FormatBits(ec) << 3) | maskPattern;
        int rem = data;
        for (int i = 0; i < 10; i++)
        {
            rem = (rem << 1) ^ ((rem >> 9) * Generator);
        }

        return ((data << 10) | rem) ^ FormatMask;
    }

    /// <summary>
    /// Writes both copies of the format information (and the always-dark module) into the matrix
    /// for the given EC level and mask pattern.
    /// </summary>
    public static void Place(QrMatrix m, EcLevel ec, int maskPattern)
    {
        if (m is null)
        {
            throw new ArgumentNullException(nameof(m));
        }

        int bits = Compute(ec, maskPattern);
        int size = m.Size;

        // Format bits are placed most-significant-bit first (bit 14 → bit 0).
        // Copy 1 — along row 8 (left→right), then up column 8.
        int k = 0;
        for (int c = 0; c <= 5; c++)
        {
            m.SetFunction(8, c, Bit(bits, 14 - k++));
        }

        m.SetFunction(8, 7, Bit(bits, 14 - k++));
        m.SetFunction(8, 8, Bit(bits, 14 - k++));
        m.SetFunction(7, 8, Bit(bits, 14 - k++));
        for (int r = 5; r >= 0; r--)
        {
            m.SetFunction(r, 8, Bit(bits, 14 - k++));
        }

        // Copy 2 — up column 8 from the bottom (bits 14–8), then along row 8 (bits 7–0).
        k = 0;
        for (int r = size - 1; r >= size - 7; r--)
        {
            m.SetFunction(r, 8, Bit(bits, 14 - k++));
        }

        for (int c = size - 8; c <= size - 1; c++)
        {
            m.SetFunction(8, c, Bit(bits, 14 - k++));
        }

        // Always-dark module at (4·version + 9, 8) = (size − 8, 8).
        m.SetFunction(size - 8, 8, true);
    }

    private static bool Bit(int value, int index) => ((value >> index) & 1) != 0;
}
