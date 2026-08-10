using System;

namespace QrGen;

/// <summary>Constant tables specific to QR Code Version 4 (33×33 modules).</summary>
public static class Version4
{
    /// <summary>Side length of the module matrix (excluding the quiet zone).</summary>
    public const int Modules = 33;

    /// <summary>Total number of codewords (data + error correction) for Version 4.</summary>
    public const int TotalCodewords = 100;

    /// <summary>Number of remainder bits appended after the interleaved codewords.</summary>
    public const int RemainderBits = 7;

    /// <summary>Center coordinate (row = col) of the single alignment pattern.</summary>
    public const int AlignmentCenter = 26;

    /// <summary>Number of data codewords available at the given EC level.</summary>
    public static int DataCodewords(EcLevel ec) => ec switch
    {
        EcLevel.L => 80,
        EcLevel.M => 64,
        EcLevel.Q => 48,
        EcLevel.H => 36,
        _ => throw new ArgumentOutOfRangeException(nameof(ec)),
    };

    /// <summary>Width of the character-count indicator for the given mode (Versions 1-9).</summary>
    public static int CharCountBits(QrMode mode) => mode switch
    {
        QrMode.Numeric => 10,
        QrMode.Alphanumeric => 9,
        QrMode.Byte => 8,
        QrMode.Kanji => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    /// <summary>
    /// Error-correction block layout for the given EC level: the number of (equal-sized)
    /// blocks, the EC codewords per block, and the data codewords per block.
    /// </summary>
    public static (int Blocks, int EcPerBlock, int DataPerBlock) BlockLayout(EcLevel ec) => ec switch
    {
        EcLevel.L => (1, 20, 80),
        EcLevel.M => (2, 18, 32),
        EcLevel.Q => (2, 26, 24),
        EcLevel.H => (4, 16, 9),
        _ => throw new ArgumentOutOfRangeException(nameof(ec)),
    };
}
