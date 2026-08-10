using System;

namespace QrGen;

/// <summary>
/// QR data masking (§7.8): the eight mask patterns, the four penalty rules used to score a
/// masked symbol, and selection of the lowest-penalty mask. Masks affect data modules only;
/// function modules are never toggled.
/// </summary>
public static class Masking
{
    /// <summary>Number of available mask patterns (0–7).</summary>
    public const int PatternCount = 8;

    private static readonly bool[] Finder1 = { true, false, true, true, true, false, true, false, false, false, false };
    private static readonly bool[] Finder2 = { false, false, false, false, true, false, true, true, true, false, true };

    /// <summary>
    /// Evaluates whether the module at (<paramref name="row"/>, <paramref name="col"/>) is
    /// toggled by the given mask pattern.
    /// </summary>
    public static bool MaskCondition(int pattern, int row, int col) => pattern switch
    {
        0 => (row + col) % 2 == 0,
        1 => row % 2 == 0,
        2 => col % 3 == 0,
        3 => (row + col) % 3 == 0,
        4 => ((row / 2) + (col / 3)) % 2 == 0,
        5 => ((row * col) % 2) + ((row * col) % 3) == 0,
        6 => (((row * col) % 2) + ((row * col) % 3)) % 2 == 0,
        7 => (((row + col) % 2) + ((row * col) % 3)) % 2 == 0,
        _ => throw new ArgumentOutOfRangeException(nameof(pattern), "Mask pattern must be 0–7."),
    };

    /// <summary>
    /// Returns a copy of <paramref name="source"/> with the given mask pattern applied to its
    /// data modules (function modules are left unchanged).
    /// </summary>
    public static QrMatrix Apply(QrMatrix source, int pattern)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var m = source.Clone();
        for (int r = 0; r < m.Size; r++)
        {
            for (int c = 0; c < m.Size; c++)
            {
                if (!m.IsFunction(r, c) && MaskCondition(pattern, r, c))
                {
                    m.SetData(r, c, !m.IsDark(r, c));
                }
            }
        }

        return m;
    }

    /// <summary>Total penalty score (sum of the four rules) for a masked symbol.</summary>
    public static int Penalty(QrMatrix m) =>
        PenaltyRule1(m) + PenaltyRule2(m) + PenaltyRule3(m) + PenaltyRule4(m);

    /// <summary>Rule 1: five or more consecutive same-colour modules in a row or column.</summary>
    public static int PenaltyRule1(QrMatrix m)
    {
        int penalty = 0;
        int size = m.Size;

        for (int r = 0; r < size; r++)
        {
            penalty += LinePenalty(m, r, isRow: true);
        }

        for (int c = 0; c < size; c++)
        {
            penalty += LinePenalty(m, c, isRow: false);
        }

        return penalty;
    }

    private static int LinePenalty(QrMatrix m, int index, bool isRow)
    {
        int size = m.Size;
        int penalty = 0;
        int runLength = 1;
        bool runColour = isRow ? m.IsDark(index, 0) : m.IsDark(0, index);

        for (int k = 1; k < size; k++)
        {
            bool colour = isRow ? m.IsDark(index, k) : m.IsDark(k, index);
            if (colour == runColour)
            {
                runLength++;
            }
            else
            {
                penalty += ScoreRun(runLength);
                runColour = colour;
                runLength = 1;
            }
        }

        penalty += ScoreRun(runLength);
        return penalty;
    }

    private static int ScoreRun(int runLength) => runLength >= 5 ? 3 + (runLength - 5) : 0;

    /// <summary>Rule 2: each 2×2 block of same-colour modules.</summary>
    public static int PenaltyRule2(QrMatrix m)
    {
        int penalty = 0;
        int size = m.Size;

        for (int r = 0; r < size - 1; r++)
        {
            for (int c = 0; c < size - 1; c++)
            {
                bool tl = m.IsDark(r, c);
                if (tl == m.IsDark(r, c + 1) && tl == m.IsDark(r + 1, c) && tl == m.IsDark(r + 1, c + 1))
                {
                    penalty += 3;
                }
            }
        }

        return penalty;
    }

    /// <summary>Rule 3: finder-like 1:1:3:1:1 patterns with four light modules on either side.</summary>
    public static int PenaltyRule3(QrMatrix m)
    {
        int penalty = 0;
        int size = m.Size;
        int window = Finder1.Length;

        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c <= size - window; c++)
            {
                if (MatchesFinder(m, r, c, isRow: true))
                {
                    penalty += 40;
                }
            }
        }

        for (int c = 0; c < size; c++)
        {
            for (int r = 0; r <= size - window; r++)
            {
                if (MatchesFinder(m, r, c, isRow: false))
                {
                    penalty += 40;
                }
            }
        }

        return penalty;
    }

    private static bool MatchesFinder(QrMatrix m, int row, int col, bool isRow)
    {
        bool match1 = true;
        bool match2 = true;
        for (int k = 0; k < Finder1.Length; k++)
        {
            bool colour = isRow ? m.IsDark(row, col + k) : m.IsDark(row + k, col);
            if (colour != Finder1[k])
            {
                match1 = false;
            }

            if (colour != Finder2[k])
            {
                match2 = false;
            }
        }

        return match1 || match2;
    }

    /// <summary>Rule 4: deviation of the dark-module proportion from 50%.</summary>
    public static int PenaltyRule4(QrMatrix m)
    {
        int total = m.Size * m.Size;
        int dark = m.CountDark();
        double percent = 100.0 * dark / total;
        int lower = (int)Math.Floor(percent / 5.0) * 5;
        int upper = (int)Math.Ceiling(percent / 5.0) * 5;
        return Math.Min(Math.Abs(lower - 50), Math.Abs(upper - 50)) / 5 * 10;
    }

    /// <summary>
    /// Applies every mask pattern and returns the pattern that yields the lowest penalty score,
    /// together with the corresponding masked matrix and its score. Ties favour the lower pattern number.
    /// </summary>
    public static (int Pattern, QrMatrix Masked, int Penalty) SelectBestMask(QrMatrix source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        int bestPattern = 0;
        QrMatrix? bestMatrix = null;
        int bestScore = int.MaxValue;

        for (int pattern = 0; pattern < PatternCount; pattern++)
        {
            QrMatrix masked = Apply(source, pattern);
            int score = Penalty(masked);
            if (score < bestScore)
            {
                bestScore = score;
                bestPattern = pattern;
                bestMatrix = masked;
            }
        }

        return (bestPattern, bestMatrix!, bestScore);
    }
}
