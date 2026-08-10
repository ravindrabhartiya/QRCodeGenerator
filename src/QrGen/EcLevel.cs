namespace QrGen;

/// <summary>QR error-correction levels, from lowest to highest recovery capacity.</summary>
public enum EcLevel
{
    /// <summary>Recovers ~7% of data.</summary>
    L,

    /// <summary>Recovers ~15% of data.</summary>
    M,

    /// <summary>Recovers ~25% of data.</summary>
    Q,

    /// <summary>Recovers ~30% of data.</summary>
    H,
}
