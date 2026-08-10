using System;
using Xunit;

namespace QrGen.Tests;

/// <summary>Unit tests for <see cref="ReedSolomon"/> (Step 3: EC codeword generation).</summary>
public class ReedSolomonTests
{
    [Fact]
    public void GeneratorPolynomial_Degree2_Is_1_3_2()
    {
        Assert.Equal(new byte[] { 1, 3, 2 }, ReedSolomon.GeneratorPolynomial(2));
    }

    [Fact]
    public void GeneratorPolynomial_DegreeN_HasNPlusOneCoefficients()
    {
        Assert.Equal(11, ReedSolomon.GeneratorPolynomial(10).Length);
        Assert.Equal(21, ReedSolomon.GeneratorPolynomial(20).Length);
    }

    [Fact]
    public void GeneratorPolynomial_IsMonic()
    {
        Assert.Equal((byte)1, ReedSolomon.GeneratorPolynomial(18)[0]);
    }

    [Fact]
    public void ComputeEc_ThonkyGoldenVector_Matches()
    {
        // Thonky "HELLO WORLD" version 1-M data codewords.
        byte[] data =
        {
            32, 91, 11, 120, 209, 114, 220, 77, 67, 64, 236, 17, 236, 17, 236, 17,
        };
        byte[] expected = { 196, 35, 39, 119, 235, 215, 231, 226, 93, 23 };

        Assert.Equal(expected, ReedSolomon.ComputeEc(data, 10));
    }

    [Fact]
    public void ComputeEc_ReturnsRequestedCount()
    {
        var data = new byte[80];
        Assert.Equal(20, ReedSolomon.ComputeEc(data, 20).Length);
    }

    [Fact]
    public void ComputeEc_NullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ReedSolomon.ComputeEc(null!, 10));
    }
}
