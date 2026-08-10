using System.Text;

namespace QrGen;

/// <summary>Provides a strict Shift JIS (code page 932) encoding shared across the library.</summary>
internal static class ShiftJisEncoding
{
    /// <summary>Shift JIS encoder/decoder that throws on unmappable characters.</summary>
    public static Encoding Strict { get; }

    static ShiftJisEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Strict = Encoding.GetEncoding(
            932,
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
    }
}
