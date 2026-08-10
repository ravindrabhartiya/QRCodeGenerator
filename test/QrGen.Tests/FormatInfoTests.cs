using System;
using Xunit;

namespace QrGen.Tests;

/// <summary>Unit tests for <see cref="FormatInfo"/> (Step 6: format-information encoding + placement).</summary>
public class FormatInfoTests
{
    // ---- FormatBits (2-bit EC indicator) ----

    [Theory]
    [InlineData(EcLevel.L, 0b01)]
    [InlineData(EcLevel.M, 0b00)]
    [InlineData(EcLevel.Q, 0b11)]
    [InlineData(EcLevel.H, 0b10)]
    public void FormatBits_ReturnsSpecIndicator(EcLevel ec, int expected) =>
        Assert.Equal(expected, FormatInfo.FormatBits(ec));

    // ---- Compute: 15-bit BCH(15,5) values against ISO/IEC 18004 Table C.1 ----

    [Theory]
    [InlineData(EcLevel.M, 0, 0b101010000010010)] // 0x5412 (the format XOR mask itself)
    [InlineData(EcLevel.L, 1, 0b111001011110011)] // 0x72F3
    [InlineData(EcLevel.Q, 0, 0b011010101011111)]
    [InlineData(EcLevel.H, 0, 0b001011010001001)]
    public void Compute_MatchesIsoTableC1(EcLevel ec, int mask, int expected) =>
        Assert.Equal(expected, FormatInfo.Compute(ec, mask));

    [Fact]
    public void Compute_ProducesFifteenBitValues()
    {
        for (int mask = 0; mask <= 7; mask++)
        {
            foreach (EcLevel ec in new[] { EcLevel.L, EcLevel.M, EcLevel.Q, EcLevel.H })
            {
                int value = FormatInfo.Compute(ec, mask);
                Assert.InRange(value, 0, 0x7FFF);
            }
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    public void Compute_ThrowsForMaskOutOfRange(int mask) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => FormatInfo.Compute(EcLevel.M, mask));

    // ---- Place: both copies carry identical bits, dark module set, function-only ----

    [Fact]
    public void Place_ThrowsOnNullMatrix() =>
        Assert.Throws<ArgumentNullException>(() => FormatInfo.Place(null!, EcLevel.M, 0));

    [Fact]
    public void Place_SetsAlwaysDarkModule()
    {
        var m = new QrMatrix(33);
        FormatInfo.Place(m, EcLevel.M, 6);

        // (4·version + 9, 8) = (25, 8) for version 4.
        Assert.True(m.IsDark(25, 8));
        Assert.True(m.IsFunction(25, 8));
    }

    [Fact]
    public void Place_BothCopiesEncodeTheSameBits()
    {
        var m = new QrMatrix(33);
        FormatInfo.Place(m, EcLevel.Q, 3);
        int size = m.Size;
        int bits = FormatInfo.Compute(EcLevel.Q, 3);

        // Copy 1 (bit 14 → bit 0): row 8 across, then column 8 up.
        var copy1 = new (int r, int c)[]
        {
            (8, 0), (8, 1), (8, 2), (8, 3), (8, 4), (8, 5), (8, 7), (8, 8),
            (7, 8), (5, 8), (4, 8), (3, 8), (2, 8), (1, 8), (0, 8),
        };

        // Copy 2 (bit 14 → bit 0): column 8 up from bottom, then row 8 across the right.
        var copy2 = new (int r, int c)[]
        {
            (size - 1, 8), (size - 2, 8), (size - 3, 8), (size - 4, 8),
            (size - 5, 8), (size - 6, 8), (size - 7, 8),
            (8, size - 8), (8, size - 7), (8, size - 6), (8, size - 5),
            (8, size - 4), (8, size - 3), (8, size - 2), (8, size - 1),
        };

        for (int i = 0; i < 15; i++)
        {
            bool expected = ((bits >> (14 - i)) & 1) != 0;
            Assert.Equal(expected, m.IsDark(copy1[i].r, copy1[i].c));
            Assert.Equal(expected, m.IsDark(copy2[i].r, copy2[i].c));
        }
    }

    [Fact]
    public void Place_MarksAllFormatModulesAsFunction()
    {
        var m = new QrMatrix(33);
        FormatInfo.Place(m, EcLevel.H, 2);
        int size = m.Size;

        for (int c = 0; c <= 8; c++)
        {
            if (c != 6)
            {
                Assert.True(m.IsFunction(8, c), $"(8,{c}) should be a function module.");
            }
        }

        for (int r = 0; r <= 8; r++)
        {
            if (r != 6)
            {
                Assert.True(m.IsFunction(r, 8), $"({r},8) should be a function module.");
            }
        }

        for (int c = size - 8; c <= size - 1; c++)
        {
            Assert.True(m.IsFunction(8, c), $"(8,{c}) should be a function module.");
        }

        for (int r = size - 7; r <= size - 1; r++)
        {
            Assert.True(m.IsFunction(r, 8), $"({r},8) should be a function module.");
        }
    }

    // ---- End-to-end golden matrix (validated 0-diff against the segno reference library) ----

    [Fact]
    public void FullPipeline_HelloCcWorld_M_MatchesGoldenMatrix()
    {
        byte[] codewords = DataEncoder.Encode("HELLO CC WORLD", QrMode.Alphanumeric, EcLevel.M);
        byte[] finalCodewords = BlockInterleaver.Interleave(codewords, EcLevel.M);
        QrMatrix matrix = MatrixBuilder.Build(finalCodewords);
        var (maskPattern, masked, _) = Masking.SelectBestMask(matrix);
        FormatInfo.Place(masked, EcLevel.M, maskPattern);

        Assert.Equal(6, maskPattern);
        Assert.Equal(GoldenHelloCcWorldM.Length, masked.Size);

        for (int r = 0; r < masked.Size; r++)
        {
            string row = GoldenHelloCcWorldM[r];
            for (int c = 0; c < masked.Size; c++)
            {
                bool expected = row[c] == '1';
                Assert.Equal(expected, masked.IsDark(r, c));
            }
        }
    }

    /// <summary>
    /// Final 33×33 matrix (masked + format info) for "HELLO CC WORLD" at EC level M, mask 6.
    /// Independently verified to be byte-identical to the segno reference library's output.
    /// </summary>
    private static readonly string[] GoldenHelloCcWorldM =
    {
        "111111101000101100110011001111111",
        "100000101001000101001111101000001",
        "101110101111000001101011001011101",
        "101110100001000100010001001011101",
        "101110101100001101101101101011101",
        "100000100100000001101011001000001",
        "111111101010101010101010101111111",
        "000000000111110000011010100000000",
        "100111111111010100111110010010111",
        "111110010000000010001000101001101",
        "100100100101001111100101010100010",
        "100100001100001011000001101111001",
        "111010101111011111111111100011100",
        "001010001011110011010110011000100",
        "011101101100111011000001111011111",
        "100001000101000101110111000010101",
        "111110111001001001011110111010101",
        "100001011011011000101111001110010",
        "111000110101101010001000111100111",
        "011000010010101111100101001100101",
        "110001100110110000001101010111110",
        "100110010010101010101010111101010",
        "100001111100101111100101010111001",
        "101100000010100011000001101100011",
        "110001110110011101110111111111101",
        "000000001010001000011010100011111",
        "111111101110010100111110101011001",
        "100000101111110010001000100010100",
        "101110101100010101101101111110110",
        "101110101010001000001101111011100",
        "101110100101000010001001000010111",
        "100000100011110111100100100100110",
        "111111101000111010000101011111110",
    };
}
