using System.IO;

namespace QrGen.Tests;

/// <summary>Unit tests for the CLI entry point <see cref="Cli.CliRunner"/>.</summary>
public class CliRunnerTests
{
    private static (int code, string stdout, string stderr) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        int code = Cli.CliRunner.Run(args, o, e);
        return (code, o.ToString(), e.ToString());
    }

    [Fact]
    public void NoArgs_ReturnsErrorAndUsage()
    {
        var (code, _, err) = Run();
        Assert.NotEqual(0, code);
        Assert.Contains("error", err);
        Assert.Contains("Usage", err);
    }

    [Fact]
    public void Help_ReturnsZeroAndUsage()
    {
        var (code, stdout, _) = Run("--help");
        Assert.Equal(0, code);
        Assert.Contains("Usage: qrgen", stdout);
    }

    [Fact]
    public void Alphanumeric_ShowsDetectedMode()
    {
        var (code, stdout, _) = Run("HELLO CC WORLD");
        Assert.Equal(0, code);
        Assert.Contains("Alphanumeric", stdout);
    }

    [Fact]
    public void EcOption_SelectsLevelAndCapacity()
    {
        var (code, stdout, _) = Run("12345", "--ec", "L");
        Assert.Equal(0, code);
        Assert.Contains("Numeric", stdout);
        Assert.Contains("EC Level:   L", stdout);
        Assert.Contains("(80)", stdout); // 80 data codewords at L
    }

    [Fact]
    public void InvalidEcValue_ReturnsError()
    {
        var (code, _, err) = Run("12345", "--ec", "X");
        Assert.NotEqual(0, code);
        Assert.Contains("invalid EC", err);
    }

    [Fact]
    public void UnknownOption_ReturnsError()
    {
        var (code, _, err) = Run("12345", "--nope");
        Assert.NotEqual(0, code);
        Assert.Contains("unknown option", err);
    }

    [Fact]
    public void Output_ShowsFinalInterleavedCodewords()
    {
        var (code, stdout, _) = Run("HELLO CC WORLD", "--ec", "M");
        Assert.Equal(0, code);
        Assert.Contains("Blocks:     2 × (32 data + 18 EC)", stdout);
        Assert.Contains("Final codewords (100)", stdout);
    }

    [Fact]
    public void Output_ShowsMatrixDimensionsAndCounts()
    {
        var (code, stdout, _) = Run("HELLO CC WORLD", "--ec", "M");
        Assert.Equal(0, code);
        Assert.Contains("Matrix:     33×33", stdout);
        Assert.Contains("807 data modules", stdout);
    }

    [Fact]
    public void NonEncodableInput_ReturnsError()
    {
        var (code, _, err) = Run("\uD83D\uDE00"); // emoji
        Assert.NotEqual(0, code);
        Assert.Contains("error", err);
    }
}
