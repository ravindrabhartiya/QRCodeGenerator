using System;
using System.IO;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace QrGen;

/// <summary>
/// Renders a finished <see cref="QrMatrix"/> for output (§7.7, §9.1): adds the mandatory
/// four-module quiet zone and produces a PNG image or an ASCII/console preview.
/// </summary>
public static class Renderer
{
    /// <summary>Standard quiet-zone width in modules (ISO/IEC 18004 §9.1).</summary>
    public const int DefaultQuietZone = 4;

    /// <summary>Default pixel size of a single module when rendering a PNG.</summary>
    public const int DefaultModuleSize = 8;

    private static readonly Rgba32 Black = new(0, 0, 0);
    private static readonly Rgba32 White = new(255, 255, 255);

    /// <summary>
    /// Expands the matrix into a dark/light grid surrounded by a light quiet zone.
    /// Returns a <c>[row, col]</c> array where <c>true</c> is a dark module.
    /// </summary>
    public static bool[,] ToGrid(QrMatrix matrix, int quietZone = DefaultQuietZone)
    {
        if (matrix is null)
        {
            throw new ArgumentNullException(nameof(matrix));
        }

        if (quietZone < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quietZone), "Quiet zone must be non-negative.");
        }

        int n = matrix.Size + 2 * quietZone;
        var grid = new bool[n, n];
        for (int r = 0; r < matrix.Size; r++)
        {
            for (int c = 0; c < matrix.Size; c++)
            {
                grid[r + quietZone, c + quietZone] = matrix.IsDark(r, c);
            }
        }

        return grid;
    }

    /// <summary>Renders the matrix (with quiet zone) to PNG bytes.</summary>
    /// <param name="matrix">The finished QR matrix.</param>
    /// <param name="moduleSize">Pixel width/height of each module (must be positive).</param>
    /// <param name="quietZone">Quiet-zone width in modules.</param>
    public static byte[] ToPng(
        QrMatrix matrix,
        int moduleSize = DefaultModuleSize,
        int quietZone = DefaultQuietZone)
    {
        using var stream = new MemoryStream();
        RenderPng(matrix, stream, moduleSize, quietZone);
        return stream.ToArray();
    }

    /// <summary>Renders the matrix (with quiet zone) to a PNG file at <paramref name="path"/>.</summary>
    public static void SavePng(
        QrMatrix matrix,
        string path,
        int moduleSize = DefaultModuleSize,
        int quietZone = DefaultQuietZone)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must be provided.", nameof(path));
        }

        using var stream = File.Create(path);
        RenderPng(matrix, stream, moduleSize, quietZone);
    }

    /// <summary>
    /// Produces a text preview of the matrix (with quiet zone), using <paramref name="dark"/> for
    /// dark modules and <paramref name="light"/> for light ones.
    /// </summary>
    public static string ToAscii(
        QrMatrix matrix,
        int quietZone = DefaultQuietZone,
        string dark = "██",
        string light = "  ")
    {
        bool[,] grid = ToGrid(matrix, quietZone);
        int n = grid.GetLength(0);
        var sb = new StringBuilder(n * (n * Math.Max(dark.Length, light.Length) + 1));
        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                sb.Append(grid[r, c] ? dark : light);
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }

    private static void RenderPng(QrMatrix matrix, Stream stream, int moduleSize, int quietZone)
    {
        if (moduleSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(moduleSize), "Module size must be positive.");
        }

        bool[,] grid = ToGrid(matrix, quietZone);
        int n = grid.GetLength(0);
        int px = n * moduleSize;

        using var image = new Image<Rgba32>(px, px);
        for (int y = 0; y < px; y++)
        {
            int gr = y / moduleSize;
            for (int x = 0; x < px; x++)
            {
                image[x, y] = grid[gr, x / moduleSize] ? Black : White;
            }
        }

        image.SaveAsPng(stream);
    }
}
