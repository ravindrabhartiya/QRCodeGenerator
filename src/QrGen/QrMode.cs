namespace QrGen;

/// <summary>The four QR Code data-encoding modes, ordered from most to least compact.</summary>
public enum QrMode
{
    /// <summary>Digits 0-9 only.</summary>
    Numeric,

    /// <summary>Digits, uppercase A-Z, space, and the symbols $ % * + - . / :.</summary>
    Alphanumeric,

    /// <summary>Single-byte characters from the ISO-8859-1 (Latin-1) character set.</summary>
    Byte,

    /// <summary>Double-byte characters from the Shift JIS character set.</summary>
    Kanji,
}
