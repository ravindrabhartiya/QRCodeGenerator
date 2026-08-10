using System;

namespace QrGen.Tests;

/// <summary>Unit tests for <see cref="ModeDetector"/> (Step 1: mode detection).</summary>
public class ModeDetectorTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("9")]
    [InlineData("12345")]
    [InlineData("007")]
    [InlineData("0000000000")]
    public void Detect_PureDigits_ReturnsNumeric(string input)
    {
        Assert.Equal(QrMode.Numeric, ModeDetector.Detect(input));
    }

    [Theory]
    [InlineData("HELLO CC WORLD")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
    [InlineData("$25.00")]
    [InlineData("A1 B2")]
    [InlineData("100%")]
    [InlineData("12:34")]
    [InlineData("-42")]
    [InlineData("+*./: $%")]
    public void Detect_UppercaseAndAllowedSymbols_ReturnsAlphanumeric(string input)
    {
        Assert.Equal(QrMode.Alphanumeric, ModeDetector.Detect(input));
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("Hello, World!")]
    [InlineData("abc123")]
    [InlineData("caf\u00E9")]   // café (é is Latin-1 0xE9)
    [InlineData("~")]           // tilde: Latin-1 but not in the alphanumeric set
    [InlineData("a")]
    public void Detect_Latin1NotAlphanumeric_ReturnsByte(string input)
    {
        Assert.Equal(QrMode.Byte, ModeDetector.Detect(input));
    }

    [Theory]
    [InlineData("\u6F22\u5B57")]              // 漢字
    [InlineData("\u65E5\u672C")]              // 日本
    [InlineData("\u4E00")]                    // 一
    [InlineData("\u3053\u3093\u306B\u3061\u306F")] // こんにちは
    public void Detect_ShiftJisDoubleByte_ReturnsKanji(string input)
    {
        Assert.Equal(QrMode.Kanji, ModeDetector.Detect(input));
    }

    [Fact]
    public void Detect_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ModeDetector.Detect(null!));
    }

    [Fact]
    public void Detect_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => ModeDetector.Detect(string.Empty));
    }

    [Theory]
    [InlineData("\uD83D\uDE00")] // 😀 emoji: not Latin-1, not Shift JIS
    [InlineData("A\u6F22")]      // 'A' + 漢: mixed, encodable by no single mode in scope
    public void Detect_NonEncodable_Throws(string input)
    {
        Assert.Throws<ArgumentException>(() => ModeDetector.Detect(input));
    }
}
