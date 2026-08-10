using System;

namespace QrGen;

/// <summary>
/// Builds the Version 4 module matrix (§7.7): finder patterns, separators, timing patterns,
/// the alignment pattern, the dark module, reserved format-information areas, and the
/// zig-zag placement of data codeword bits. Masking (Step 5) and format-information
/// content (Step 6) are applied later.
/// </summary>
public static class MatrixBuilder
{
    /// <summary>
    /// Builds a Version 4 matrix from the final (interleaved) codewords, placing all
    /// function patterns and the unmasked data bits.
    /// </summary>
    /// <param name="finalCodewords">The 100 interleaved codewords from <see cref="BlockInterleaver"/>.</param>
    public static QrMatrix Build(byte[] finalCodewords)
    {
        if (finalCodewords is null)
        {
            throw new ArgumentNullException(nameof(finalCodewords));
        }

        if (finalCodewords.Length != Version4.TotalCodewords)
        {
            throw new ArgumentException(
                $"Expected {Version4.TotalCodewords} codewords, got {finalCodewords.Length}.",
                nameof(finalCodewords));
        }

        var m = new QrMatrix(Version4.Modules);
        PlaceFinderPatterns(m);
        PlaceSeparators(m);
        PlaceTimingPatterns(m);
        PlaceAlignmentPattern(m);
        PlaceDarkModule(m);
        ReserveFormatInfo(m);
        PlaceData(m, ToBits(finalCodewords, Version4.RemainderBits));
        return m;
    }

    /// <summary>Places the three 7×7 finder patterns in the top-left, top-right and bottom-left corners.</summary>
    public static void PlaceFinderPatterns(QrMatrix m)
    {
        PlaceFinder(m, 0, 0);
        PlaceFinder(m, 0, m.Size - 7);
        PlaceFinder(m, m.Size - 7, 0);
    }

    private static void PlaceFinder(QrMatrix m, int top, int left)
    {
        for (int r = 0; r < 7; r++)
        {
            for (int c = 0; c < 7; c++)
            {
                bool dark = r == 0 || r == 6 || c == 0 || c == 6
                            || (r >= 2 && r <= 4 && c >= 2 && c <= 4);
                m.SetFunction(top + r, left + c, dark);
            }
        }
    }

    /// <summary>Places the one-module light separators bordering each finder pattern.</summary>
    public static void PlaceSeparators(QrMatrix m)
    {
        int size = m.Size;

        for (int c = 0; c <= 7; c++)
        {
            m.SetFunction(7, c, false);
        }

        for (int r = 0; r <= 7; r++)
        {
            m.SetFunction(r, 7, false);
        }

        for (int c = size - 8; c < size; c++)
        {
            m.SetFunction(7, c, false);
        }

        for (int r = 0; r <= 7; r++)
        {
            m.SetFunction(r, size - 8, false);
        }

        for (int c = 0; c <= 7; c++)
        {
            m.SetFunction(size - 8, c, false);
        }

        for (int r = size - 8; r < size; r++)
        {
            m.SetFunction(r, 7, false);
        }
    }

    /// <summary>Places the horizontal (row 6) and vertical (col 6) timing patterns.</summary>
    public static void PlaceTimingPatterns(QrMatrix m)
    {
        int size = m.Size;
        for (int c = 8; c < size - 8; c++)
        {
            m.SetFunction(6, c, c % 2 == 0);
        }

        for (int r = 8; r < size - 8; r++)
        {
            m.SetFunction(r, 6, r % 2 == 0);
        }
    }

    /// <summary>Places the single 5×5 alignment pattern centred at (26, 26).</summary>
    public static void PlaceAlignmentPattern(QrMatrix m)
    {
        int center = Version4.AlignmentCenter;
        for (int dr = -2; dr <= 2; dr++)
        {
            for (int dc = -2; dc <= 2; dc++)
            {
                bool dark = Math.Max(Math.Abs(dr), Math.Abs(dc)) != 1;
                m.SetFunction(center + dr, center + dc, dark);
            }
        }
    }

    /// <summary>Places the always-dark module at (4·version + 9, 8) = (25, 8) for Version 4.</summary>
    public static void PlaceDarkModule(QrMatrix m)
    {
        m.SetFunction(m.Size - 8, 8, true);
    }

    /// <summary>
    /// Reserves (marks as function, colour to be filled in Step 6) the two format-information
    /// strips around the finder patterns, without disturbing the timing modules they cross.
    /// </summary>
    public static void ReserveFormatInfo(QrMatrix m)
    {
        int size = m.Size;

        for (int c = 0; c <= 8; c++)
        {
            Reserve(m, 8, c);
        }

        for (int r = 0; r <= 8; r++)
        {
            Reserve(m, r, 8);
        }

        for (int c = size - 8; c < size; c++)
        {
            Reserve(m, 8, c);
        }

        for (int r = size - 7; r < size; r++)
        {
            Reserve(m, r, 8);
        }
    }

    private static void Reserve(QrMatrix m, int row, int col)
    {
        if (!m.IsFunction(row, col))
        {
            m.SetFunction(row, col, false);
        }
    }

    /// <summary>
    /// Places the data bits in the standard upward/downward zig-zag of two-module-wide columns,
    /// starting from the bottom-right corner and skipping function modules and the timing column.
    /// </summary>
    public static void PlaceData(QrMatrix m, bool[] bits)
    {
        if (bits is null)
        {
            throw new ArgumentNullException(nameof(bits));
        }

        int size = m.Size;
        int bitIndex = 0;
        bool upward = true;

        for (int col = size - 1; col > 0; col -= 2)
        {
            if (col == 6)
            {
                col--;
            }

            for (int i = 0; i < size; i++)
            {
                int row = upward ? size - 1 - i : i;
                for (int j = 0; j < 2; j++)
                {
                    int c = col - j;
                    if (!m.IsFunction(row, c))
                    {
                        bool dark = bitIndex < bits.Length && bits[bitIndex];
                        m.SetData(row, c, dark);
                        bitIndex++;
                    }
                }
            }

            upward = !upward;
        }
    }

    /// <summary>
    /// Expands codewords (MSB-first) into a bit array, appending the given number of
    /// zero remainder bits.
    /// </summary>
    public static bool[] ToBits(byte[] codewords, int remainderBits)
    {
        if (codewords is null)
        {
            throw new ArgumentNullException(nameof(codewords));
        }

        var bits = new bool[(codewords.Length * 8) + remainderBits];
        int index = 0;
        foreach (byte b in codewords)
        {
            for (int bit = 7; bit >= 0; bit--)
            {
                bits[index++] = ((b >> bit) & 1) != 0;
            }
        }

        return bits;
    }
}
