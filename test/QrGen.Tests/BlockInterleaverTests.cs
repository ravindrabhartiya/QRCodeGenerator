using System;
using System.Linq;
using Xunit;

namespace QrGen.Tests;

/// <summary>Unit tests for <see cref="BlockInterleaver"/> (Step 3: block split + interleave).</summary>
public class BlockInterleaverTests
{
    [Fact]
    public void Interleave_L_SingleBlock_DataThenEc()
    {
        var data = new byte[80];
        for (int i = 0; i < 80; i++)
        {
            data[i] = (byte)i;
        }

        byte[] result = BlockInterleaver.Interleave(data, EcLevel.L);

        Assert.Equal(100, result.Length);
        Assert.Equal(data, result.Take(80).ToArray());
        Assert.Equal(ReedSolomon.ComputeEc(data, 20), result.Skip(80).ToArray());
    }

    [Fact]
    public void Interleave_M_TwoBlocks_DataColumnInterleaved()
    {
        var data = new byte[64];
        for (int i = 0; i < 64; i++)
        {
            data[i] = (byte)i;
        }

        byte[] result = BlockInterleaver.Interleave(data, EcLevel.M);

        Assert.Equal(100, result.Length);

        // First 64 bytes are the data codewords, interleaved column-wise:
        // block0[0], block1[0], block0[1], block1[1], ...
        for (int i = 0; i < 32; i++)
        {
            Assert.Equal((byte)i, result[i * 2]);       // block 0 value i
            Assert.Equal((byte)(i + 32), result[i * 2 + 1]); // block 1 value i+32
        }
    }

    [Fact]
    public void Interleave_M_EcIsColumnInterleaved()
    {
        var data = new byte[64];
        for (int i = 0; i < 64; i++)
        {
            data[i] = (byte)(i * 3);
        }

        byte[] block0Ec = ReedSolomon.ComputeEc(data.Take(32).ToArray(), 18);
        byte[] block1Ec = ReedSolomon.ComputeEc(data.Skip(32).ToArray(), 18);

        byte[] result = BlockInterleaver.Interleave(data, EcLevel.M);
        byte[] ecPortion = result.Skip(64).ToArray();

        Assert.Equal(36, ecPortion.Length);
        for (int i = 0; i < 18; i++)
        {
            Assert.Equal(block0Ec[i], ecPortion[i * 2]);
            Assert.Equal(block1Ec[i], ecPortion[i * 2 + 1]);
        }
    }

    [Theory]
    [InlineData(EcLevel.L, 80)]
    [InlineData(EcLevel.M, 64)]
    [InlineData(EcLevel.Q, 48)]
    [InlineData(EcLevel.H, 36)]
    public void Interleave_AlwaysReturns100Codewords(EcLevel ec, int dataLen)
    {
        var data = new byte[dataLen];
        Assert.Equal(100, BlockInterleaver.Interleave(data, ec).Length);
    }

    [Fact]
    public void Interleave_WrongLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => BlockInterleaver.Interleave(new byte[10], EcLevel.M));
    }

    [Fact]
    public void Interleave_NullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BlockInterleaver.Interleave(null!, EcLevel.L));
    }
}
