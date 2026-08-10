using System;
using Xunit;

namespace QrGen.Tests;

/// <summary>Unit tests for <see cref="MatrixBuilder"/> (Step 4: matrix construction).</summary>
public class MatrixBuilderTests
{
    private const int Size = 33;

    private static QrMatrix EmptyWithFunctions(Action<QrMatrix> place)
    {
        var m = new QrMatrix(Size);
        place(m);
        return m;
    }

    // ---- Finder patterns ----

    [Theory]
    [InlineData(0, 0)]     // top-left
    [InlineData(0, 26)]    // top-right
    [InlineData(26, 0)]    // bottom-left
    public void FinderPattern_HasCorrectShape(int top, int left)
    {
        var m = EmptyWithFunctions(MatrixBuilder.PlaceFinderPatterns);

        // Outer border dark, inner ring light, 3×3 centre dark.
        Assert.True(m.IsDark(top + 0, left + 0));
        Assert.True(m.IsDark(top + 6, left + 6));
        Assert.False(m.IsDark(top + 1, left + 1));
        Assert.False(m.IsDark(top + 1, left + 5));
        Assert.True(m.IsDark(top + 3, left + 3)); // centre

        for (int r = 0; r < 7; r++)
        {
            for (int c = 0; c < 7; c++)
            {
                Assert.True(m.IsFunction(top + r, left + c));
            }
        }
    }

    // ---- Separators ----

    [Fact]
    public void Separators_AreLightAndReserved()
    {
        var m = EmptyWithFunctions(x =>
        {
            MatrixBuilder.PlaceFinderPatterns(x);
            MatrixBuilder.PlaceSeparators(x);
        });

        // Top-left separator column 7 and row 7.
        Assert.True(m.IsFunction(7, 0));
        Assert.False(m.IsDark(7, 0));
        Assert.True(m.IsFunction(0, 7));
        Assert.False(m.IsDark(0, 7));
        // Top-right separator column 25.
        Assert.True(m.IsFunction(3, 25));
        Assert.False(m.IsDark(3, 25));
        // Bottom-left separator row 25.
        Assert.True(m.IsFunction(25, 3));
        Assert.False(m.IsDark(25, 3));
    }

    // ---- Timing patterns ----

    [Fact]
    public void TimingPatterns_AlternateStartingDark()
    {
        var m = EmptyWithFunctions(MatrixBuilder.PlaceTimingPatterns);

        Assert.True(m.IsDark(6, 8));   // col 8 even → dark
        Assert.False(m.IsDark(6, 9));  // col 9 odd  → light
        Assert.True(m.IsDark(6, 10));
        Assert.True(m.IsDark(8, 6));   // row 8 even → dark
        Assert.False(m.IsDark(9, 6));

        for (int c = 8; c < Size - 8; c++)
        {
            Assert.True(m.IsFunction(6, c));
        }
    }

    // ---- Alignment pattern ----

    [Fact]
    public void AlignmentPattern_CentredAt26_HasRingAndCentre()
    {
        var m = EmptyWithFunctions(MatrixBuilder.PlaceAlignmentPattern);

        Assert.True(m.IsDark(26, 26));  // centre dark
        Assert.False(m.IsDark(25, 25)); // ring of light around centre
        Assert.False(m.IsDark(26, 25));
        Assert.True(m.IsDark(24, 24));  // outer border dark
        Assert.True(m.IsDark(28, 28));
        Assert.True(m.IsFunction(24, 24));
    }

    // ---- Dark module ----

    [Fact]
    public void DarkModule_IsAt25_8_AndDark()
    {
        var m = EmptyWithFunctions(MatrixBuilder.PlaceDarkModule);
        Assert.True(m.IsDark(25, 8));
        Assert.True(m.IsFunction(25, 8));
    }

    // ---- Format info reservation ----

    [Fact]
    public void ReserveFormatInfo_ReservesStripsWithoutBreakingTiming()
    {
        var m = EmptyWithFunctions(x =>
        {
            MatrixBuilder.PlaceFinderPatterns(x);
            MatrixBuilder.PlaceSeparators(x);
            MatrixBuilder.PlaceTimingPatterns(x);
            MatrixBuilder.PlaceDarkModule(x);
            MatrixBuilder.ReserveFormatInfo(x);
        });

        Assert.True(m.IsFunction(8, 0));
        Assert.True(m.IsFunction(0, 8));
        Assert.True(m.IsFunction(8, 8));
        Assert.True(m.IsFunction(8, Size - 1));
        Assert.True(m.IsFunction(Size - 1, 8));
        // Timing modules crossing the strips keep their colour.
        Assert.True(m.IsDark(8, 6));   // vertical timing, row 8 even
        Assert.True(m.IsDark(6, 8));   // horizontal timing, col 8 even
        // Dark module is preserved (not overwritten by the vertical strip).
        Assert.True(m.IsDark(25, 8));
    }

    // ---- ToBits ----

    [Fact]
    public void ToBits_ExpandsMsbFirst_WithRemainder()
    {
        bool[] bits = MatrixBuilder.ToBits(new byte[] { 0xA0 }, 3);
        Assert.Equal(11, bits.Length);
        Assert.True(bits[0]);   // 1
        Assert.False(bits[1]);  // 0
        Assert.True(bits[2]);   // 1
        Assert.False(bits[3]);
        Assert.False(bits[8]);  // remainder zeros
        Assert.False(bits[10]);
    }

    // ---- Full build ----

    [Fact]
    public void Build_ProducesExactly807DataModules()
    {
        byte[] codewords = BuildFinalCodewords();
        QrMatrix m = MatrixBuilder.Build(codewords);
        Assert.Equal(807, m.CountData());
        Assert.Equal(282, m.CountFunction());
    }

    [Fact]
    public void Build_PlacesFirstDataBitsAtBottomRight()
    {
        byte[] codewords = BuildFinalCodewords();
        QrMatrix m = MatrixBuilder.Build(codewords);

        // codewords[0] == 0x20 == 0b00100000. Placement order for the first column pair
        // (upward): (32,32)=bit0, (32,31)=bit1, (31,32)=bit2, (31,31)=bit3.
        Assert.False(m.IsDark(32, 32)); // bit0 = 0
        Assert.False(m.IsDark(32, 31)); // bit1 = 0
        Assert.True(m.IsDark(31, 32));  // bit2 = 1
        Assert.False(m.IsDark(31, 31)); // bit3 = 0
    }

    [Fact]
    public void Build_RejectsWrongCodewordCount()
    {
        Assert.Throws<ArgumentException>(() => MatrixBuilder.Build(new byte[10]));
    }

    private static byte[] BuildFinalCodewords()
    {
        byte[] data = DataEncoder.Encode("HELLO CC WORLD", QrMode.Alphanumeric, EcLevel.M);
        return BlockInterleaver.Interleave(data, EcLevel.M);
    }
}
