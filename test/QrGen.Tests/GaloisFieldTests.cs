using System;
using Xunit;

namespace QrGen.Tests;

/// <summary>Unit tests for <see cref="GaloisField"/> (Step 3: GF(256) arithmetic).</summary>
public class GaloisFieldTests
{
    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(5, 0, 0)]
    [InlineData(1, 200, 200)]
    [InlineData(200, 1, 200)]
    [InlineData(2, 128, 29)]
    public void Multiply_KnownValues(int a, int b, int expected)
    {
        Assert.Equal((byte)expected, GaloisField.Multiply(a, b));
    }

    [Fact]
    public void Multiply_IsCommutative()
    {
        for (int a = 0; a < 256; a += 37)
        {
            for (int b = 0; b < 256; b += 41)
            {
                Assert.Equal(GaloisField.Multiply(a, b), GaloisField.Multiply(b, a));
            }
        }
    }

    [Fact]
    public void ExpLog_RoundTrip_ForAllNonZeroElements()
    {
        for (int v = 1; v < 256; v++)
        {
            Assert.Equal((byte)v, GaloisField.Exp(GaloisField.Log(v)));
        }
    }

    [Fact]
    public void Exp_WrapsModulo255()
    {
        Assert.Equal(GaloisField.Exp(0), GaloisField.Exp(255));
        Assert.Equal(GaloisField.Exp(1), GaloisField.Exp(256));
    }

    [Fact]
    public void Exp8_Is0x1D()
    {
        Assert.Equal((byte)0x1D, GaloisField.Exp(8));
    }

    [Fact]
    public void Log_Zero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GaloisField.Log(0));
    }
}
