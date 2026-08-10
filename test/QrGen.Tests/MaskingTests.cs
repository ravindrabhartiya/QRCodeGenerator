using System;
using Xunit;

namespace QrGen.Tests;

/// <summary>Unit tests for <see cref="Masking"/> (Step 5: data masking + penalty selection).</summary>
public class MaskingTests
{
    private static QrMatrix DataMatrix(int size, Func<int, int, bool> dark)
    {
        var m = new QrMatrix(size);
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                m.SetData(r, c, dark(r, c));
            }
        }

        return m;
    }

    private static byte[] FinalCodewords() =>
        BlockInterleaver.Interleave(
            DataEncoder.Encode("HELLO CC WORLD", QrMode.Alphanumeric, EcLevel.M), EcLevel.M);

    // ---- Mask condition formulas ----

    [Theory]
    [InlineData(0, 0, 0, true)]
    [InlineData(0, 0, 1, false)]
    [InlineData(1, 0, 5, true)]
    [InlineData(1, 1, 5, false)]
    [InlineData(2, 4, 3, true)]
    [InlineData(2, 4, 1, false)]
    [InlineData(3, 1, 2, true)]
    [InlineData(4, 0, 0, true)]
    [InlineData(4, 2, 0, false)]
    [InlineData(5, 0, 0, true)]
    [InlineData(6, 1, 1, true)]
    [InlineData(7, 1, 1, false)]
    public void MaskCondition_MatchesSpecFormulas(int pattern, int row, int col, bool expected)
    {
        Assert.Equal(expected, Masking.MaskCondition(pattern, row, col));
    }

    [Fact]
    public void MaskCondition_RejectsOutOfRangePattern()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Masking.MaskCondition(8, 0, 0));
    }

    // ---- Apply ----

    [Fact]
    public void Apply_FlipsOnlyDataModules_LeavingFunctionUntouched()
    {
        QrMatrix source = MatrixBuilder.Build(FinalCodewords());
        QrMatrix masked = Masking.Apply(source, 0);

        Assert.Equal(source.CountFunction(), masked.CountFunction());
        for (int r = 0; r < source.Size; r++)
        {
            for (int c = 0; c < source.Size; c++)
            {
                if (source.IsFunction(r, c))
                {
                    Assert.Equal(source.IsDark(r, c), masked.IsDark(r, c));
                }
                else if (Masking.MaskCondition(0, r, c))
                {
                    Assert.NotEqual(source.IsDark(r, c), masked.IsDark(r, c));
                }
            }
        }
    }

    [Fact]
    public void Apply_IsInvolution_OnDataModules()
    {
        QrMatrix source = MatrixBuilder.Build(FinalCodewords());
        QrMatrix twice = Masking.Apply(Masking.Apply(source, 5), 5);

        for (int r = 0; r < source.Size; r++)
        {
            for (int c = 0; c < source.Size; c++)
            {
                Assert.Equal(source.IsDark(r, c), twice.IsDark(r, c));
            }
        }
    }

    // ---- Penalty rules ----

    [Fact]
    public void PenaltyRule1_AllLight5x5_Is30()
    {
        var m = DataMatrix(5, (_, _) => false);
        Assert.Equal(30, Masking.PenaltyRule1(m)); // 5 rows + 5 cols, each a run of 5 → 3
    }

    [Fact]
    public void PenaltyRule2_AllLight5x5_Is48()
    {
        var m = DataMatrix(5, (_, _) => false);
        Assert.Equal(48, Masking.PenaltyRule2(m)); // 4×4 blocks × 3
    }

    [Fact]
    public void PenaltyRule2_SingleBlock_Is3()
    {
        var m = DataMatrix(2, (_, _) => true);
        Assert.Equal(3, Masking.PenaltyRule2(m));
    }

    [Fact]
    public void PenaltyRule3_FinderPatternInRow_Is40()
    {
        bool[] pattern = { true, false, true, true, true, false, true, false, false, false, false };
        var m = DataMatrix(11, (r, c) => r == 0 && pattern[c]);
        Assert.Equal(40, Masking.PenaltyRule3(m));
    }

    [Fact]
    public void PenaltyRule4_AllLight_Is100()
    {
        var m = DataMatrix(5, (_, _) => false);
        Assert.Equal(100, Masking.PenaltyRule4(m)); // 0% dark → 50 away → ×2 → 100
    }

    [Fact]
    public void PenaltyRule4_HalfDark_Is0()
    {
        var m = DataMatrix(2, (r, c) => (r + c) % 2 == 0); // exactly 50% dark
        Assert.Equal(0, Masking.PenaltyRule4(m));
    }

    // ---- Selection ----

    [Fact]
    public void SelectBestMask_ReturnsLowestPenaltyPattern()
    {
        QrMatrix source = MatrixBuilder.Build(FinalCodewords());
        var (pattern, masked, penalty) = Masking.SelectBestMask(source);

        Assert.InRange(pattern, 0, 7);
        Assert.Equal(penalty, Masking.Penalty(masked));
        for (int p = 0; p < Masking.PatternCount; p++)
        {
            Assert.True(penalty <= Masking.Penalty(Masking.Apply(source, p)));
        }
    }
}
