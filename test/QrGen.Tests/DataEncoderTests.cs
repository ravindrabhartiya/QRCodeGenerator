using System;

namespace QrGen.Tests;

/// <summary>Unit tests for <see cref="DataEncoder"/> (Step 2: data encoding).</summary>
public class DataEncoderTests
{
    // ---- EncodeSegment: golden vectors (mode + count + data) ----

    [Fact]
    public void EncodeSegment_Alphanumeric_HelloCcWorld_MatchesChallengeGolden()
    {
        const string data =
            "01100001011011110001101000101110001000101000110011101001000101001101110111110";
        string expected = "0010" + "000001110" + data;

        var seg = DataEncoder.EncodeSegment("HELLO CC WORLD", QrMode.Alphanumeric);

        Assert.Equal(expected, seg.ToBitString());
    }

    [Fact]
    public void EncodeSegment_Numeric_01234567_MatchesGolden()
    {
        // 012=10bits, 345=10bits, 67=7bits
        string data = "0000001100" + "0101011001" + "1000011";
        string expected = "0001" + "0000001000" + data;

        var seg = DataEncoder.EncodeSegment("01234567", QrMode.Numeric);

        Assert.Equal(expected, seg.ToBitString());
    }

    [Theory]
    [InlineData("8", "1000")]              // single trailing digit -> 4 bits
    [InlineData("80", "1010000")]          // two trailing digits -> 7 bits
    public void EncodeSegment_Numeric_RemainderWidths(string input, string data)
    {
        string expected = "0001" + ToBinary(input.Length, 10) + data;

        var seg = DataEncoder.EncodeSegment(input, QrMode.Numeric);

        Assert.Equal(expected, seg.ToBitString());
    }

    [Fact]
    public void EncodeSegment_Alphanumeric_OddLength_UsesSixBitTail()
    {
        // (A,B)=461 -> 11 bits; C=12 -> 6 bits
        string data = "00111001101" + "001100";
        string expected = "0010" + ToBinary(3, 9) + data;

        var seg = DataEncoder.EncodeSegment("ABC", QrMode.Alphanumeric);

        Assert.Equal(expected, seg.ToBitString());
    }

    [Fact]
    public void EncodeSegment_Byte_Hello_MatchesGolden()
    {
        // h=0x68 e=0x65 l=0x6C l=0x6C o=0x6F
        string data = "01101000" + "01100101" + "01101100" + "01101100" + "01101111";
        string expected = "0100" + ToBinary(5, 8) + data;

        var seg = DataEncoder.EncodeSegment("hello", QrMode.Byte);

        Assert.Equal(expected, seg.ToBitString());
    }

    [Fact]
    public void EncodeSegment_Kanji_SingleChar_MatchesSpecExample()
    {
        // U+70B9 '点' -> Shift JIS 0x935F -> 0110110011111
        string data = "0110110011111";
        string expected = "1000" + ToBinary(1, 8) + data;

        var seg = DataEncoder.EncodeSegment("\u70B9", QrMode.Kanji);

        Assert.Equal(expected, seg.ToBitString());
    }

    // ---- PadToCapacity: terminator / alignment / pad-byte branches ----

    [Fact]
    public void PadToCapacity_ExactFitAfterTerminator_NoPadBytes()
    {
        var b = new BitBuffer();
        b.AppendBits(0b0010, 4);                 // 4 bits, gap 4 -> full terminator
        byte[] result = DataEncoder.PadToCapacity(b, 8);
        Assert.Equal(new byte[] { 0x20 }, result);
    }

    [Fact]
    public void PadToCapacity_AddsAlternatingPadBytes()
    {
        var b = new BitBuffer();
        b.AppendBits(0b0010, 4);
        byte[] result = DataEncoder.PadToCapacity(b, 24);
        Assert.Equal(new byte[] { 0x20, 0xEC, 0x11 }, result);
    }

    [Theory]
    [InlineData(6)]   // gap 2 -> terminator shortened to 2
    [InlineData(5)]   // gap 3 -> terminator shortened to 3
    public void PadToCapacity_ShortensTerminatorWhenNearCapacity(int startBits)
    {
        var b = new BitBuffer();
        b.AppendBits(1, startBits); // value fits; content irrelevant except MSB pattern
        byte[] result = DataEncoder.PadToCapacity(b, 8);
        Assert.Single(result); // exactly one byte, no 0xEC/0x11 padding
    }

    [Fact]
    public void PadToCapacity_SegmentExceedsCapacity_Throws()
    {
        var b = new BitBuffer();
        b.AppendBits(0xFFF, 12);
        Assert.Throws<ArgumentException>(() => DataEncoder.PadToCapacity(b, 8));
    }

    // ---- Encode: full Version-4 codewords ----

    [Fact]
    public void Encode_FillsCapacity_WithHeaderAndPadTail()
    {
        byte[] cw = DataEncoder.Encode("HELLO CC WORLD", QrMode.Alphanumeric, EcLevel.M);

        Assert.Equal(Version4.DataCodewords(EcLevel.M), cw.Length); // 64
        Assert.Equal(0x20, cw[0]);   // first byte = 0010 0000
        Assert.Equal(0xEC, cw[12]);  // padding starts after byte-aligned segment
        Assert.Equal(0x11, cw[13]);
        Assert.Equal(0x11, cw[^1]);  // 52 pad bytes -> last is 0x11
    }

    [Theory]
    [InlineData(EcLevel.L, 80)]
    [InlineData(EcLevel.M, 64)]
    [InlineData(EcLevel.Q, 48)]
    [InlineData(EcLevel.H, 36)]
    public void Encode_OutputLengthEqualsCapacity(EcLevel ec, int expectedLen)
    {
        byte[] cw = DataEncoder.Encode("12345", ec);
        Assert.Equal(expectedLen, cw.Length);
    }

    [Fact]
    public void Encode_AutoDetectsMode()
    {
        byte[] cw = DataEncoder.Encode("HELLO CC WORLD", EcLevel.M);
        Assert.Equal(0x20, cw[0]); // alphanumeric header detected
    }

    [Fact]
    public void Encode_ExceedsCapacity_Throws()
    {
        string tooLong = new string('1', 300); // > 82 numeric chars at H
        Assert.Throws<ArgumentException>(
            () => DataEncoder.Encode(tooLong, QrMode.Numeric, EcLevel.H));
    }

    // ---- helpers ----

    private static string ToBinary(int value, int width)
        => Convert.ToString(value, 2).PadLeft(width, '0');
}
