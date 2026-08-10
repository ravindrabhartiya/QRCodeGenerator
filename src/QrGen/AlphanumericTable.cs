namespace QrGen;

/// <summary>
/// The 45-character Alphanumeric mode table where a character's value equals its index.
/// </summary>
internal static class AlphanumericTable
{
    /// <summary>Ordered set of encodable characters (index = numeric value).</summary>
    public const string Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    /// <summary>Returns the alphanumeric value of <paramref name="c"/>, or -1 if not in the set.</summary>
    public static int Value(char c) => Chars.IndexOf(c);

    /// <summary>Returns whether <paramref name="c"/> is encodable in Alphanumeric mode.</summary>
    public static bool Contains(char c) => Chars.IndexOf(c) >= 0;
}
