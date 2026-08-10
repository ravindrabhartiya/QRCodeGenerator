using System;

namespace QrGen.Tests;

/// <summary>Unit tests for the <see cref="BitBuffer"/> utility.</summary>
public class BitBufferTests
{
    [Fact]
    public void AppendBits_MsbFirst_ProducesExpectedString()
    {
        var b = new BitBuffer();
        b.AppendBits(0b101, 3);
        Assert.Equal("101", b.ToBitString());
        Assert.Equal(3, b.Count);
    }

    [Fact]
    public void AppendBits_LeftPadsToWidth()
    {
        var b = new BitBuffer();
        b.AppendBits(5, 8);
        Assert.Equal("00000101", b.ToBitString());
    }

    [Fact]
    public void AppendByte_PacksRoundTrip()
    {
        var b = new BitBuffer();
        b.AppendByte(0x41);
        b.AppendByte(0x42);
        Assert.Equal(new byte[] { 0x41, 0x42 }, b.ToBytes());
    }

    [Fact]
    public void ToBytes_NotByteAligned_Throws()
    {
        var b = new BitBuffer();
        b.AppendBits(1, 3);
        Assert.Throws<InvalidOperationException>(() => b.ToBytes());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(32)]
    public void AppendBits_InvalidBitCount_Throws(int bitCount)
    {
        var b = new BitBuffer();
        Assert.Throws<ArgumentOutOfRangeException>(() => b.AppendBits(1, bitCount));
    }

    [Fact]
    public void AppendBits_ValueTooLargeForWidth_Throws()
    {
        var b = new BitBuffer();
        Assert.Throws<ArgumentOutOfRangeException>(() => b.AppendBits(8, 3));
    }
}
