using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace QrGen.Tests;

/// <summary>Unit tests for <see cref="Renderer"/> (Step 7: quiet zone, PNG, ASCII output).</summary>
public class RendererTests
{
    private static QrMatrix SingleDarkModule()
    {
        var m = new QrMatrix(1);
        m.SetData(0, 0, true);
        return m;
    }

    private static QrMatrix Checker(int size)
    {
        var m = new QrMatrix(size);
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                m.SetData(r, c, ((r + c) & 1) == 0);
            }
        }

        return m;
    }

    // ---- ToGrid: quiet zone + dark/light mapping ----

    [Fact]
    public void ToGrid_AddsQuietZoneOnAllSides()
    {
        bool[,] grid = Renderer.ToGrid(SingleDarkModule(), quietZone: 4);

        Assert.Equal(9, grid.GetLength(0)); // 1 + 2*4
        Assert.Equal(9, grid.GetLength(1));
        Assert.True(grid[4, 4]); // the single dark module, centered
    }

    [Fact]
    public void ToGrid_QuietZoneModulesAreAllLight()
    {
        bool[,] grid = Renderer.ToGrid(SingleDarkModule(), quietZone: 4);
        int n = grid.GetLength(0);

        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                bool inQuietZone = r < 4 || r >= n - 4 || c < 4 || c >= n - 4;
                if (inQuietZone)
                {
                    Assert.False(grid[r, c], $"quiet-zone module ({r},{c}) must be light.");
                }
            }
        }
    }

    [Fact]
    public void ToGrid_PreservesModuleColoursAtOffset()
    {
        var m = Checker(3);
        bool[,] grid = Renderer.ToGrid(m, quietZone: 4);

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                Assert.Equal(m.IsDark(r, c), grid[r + 4, c + 4]);
            }
        }
    }

    [Fact]
    public void ToGrid_ZeroQuietZoneMatchesMatrix()
    {
        var m = Checker(5);
        bool[,] grid = Renderer.ToGrid(m, quietZone: 0);

        Assert.Equal(5, grid.GetLength(0));
        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 5; c++)
            {
                Assert.Equal(m.IsDark(r, c), grid[r, c]);
            }
        }
    }

    [Fact]
    public void ToGrid_ThrowsForNegativeQuietZone() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Renderer.ToGrid(SingleDarkModule(), -1));

    [Fact]
    public void ToGrid_ThrowsForNullMatrix() =>
        Assert.Throws<ArgumentNullException>(() => Renderer.ToGrid(null!, 4));

    // ---- ToPng: dimensions + pixel colours (1 = black, 0 = white) ----

    [Fact]
    public void ToPng_HasExpectedPixelDimensions()
    {
        byte[] png = Renderer.ToPng(SingleDarkModule(), moduleSize: 8, quietZone: 4);
        using Image<Rgba32> image = Image.Load<Rgba32>(png);

        int expected = (1 + 2 * 4) * 8; // 72
        Assert.Equal(expected, image.Width);
        Assert.Equal(expected, image.Height);
    }

    [Fact]
    public void ToPng_MapsDarkToBlackAndLightToWhite()
    {
        byte[] png = Renderer.ToPng(SingleDarkModule(), moduleSize: 8, quietZone: 4);
        using Image<Rgba32> image = Image.Load<Rgba32>(png);

        // Top-left pixel is quiet zone → white.
        Assert.Equal(new Rgba32(255, 255, 255), image[0, 0]);

        // Center of the single dark module (grid cell (4,4)) → black.
        int center = 4 * 8 + 4; // 36
        Assert.Equal(new Rgba32(0, 0, 0), image[center, center]);
    }

    [Fact]
    public void ToPng_ScalesEveryModuleToModuleSizeSquare()
    {
        byte[] png = Renderer.ToPng(SingleDarkModule(), moduleSize: 8, quietZone: 4);
        using Image<Rgba32> image = Image.Load<Rgba32>(png);

        var black = new Rgba32(0, 0, 0);
        // The whole dark module spans pixels [32,40) in both axes.
        for (int y = 32; y < 40; y++)
        {
            for (int x = 32; x < 40; x++)
            {
                Assert.Equal(black, image[x, y]);
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void ToPng_ThrowsForNonPositiveModuleSize(int moduleSize) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Renderer.ToPng(SingleDarkModule(), moduleSize, 4));

    // ---- SavePng: round-trips to a real file ----

    [Fact]
    public void SavePng_WritesReadablePngFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"qrgen_{Guid.NewGuid():N}.png");
        try
        {
            Renderer.SavePng(SingleDarkModule(), path, moduleSize: 4, quietZone: 4);
            Assert.True(File.Exists(path));

            using Image<Rgba32> image = Image.Load<Rgba32>(path);
            Assert.Equal((1 + 2 * 4) * 4, image.Width);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    // ---- ToAscii: text preview with quiet zone ----

    [Fact]
    public void ToAscii_RendersQuietZoneAndModules()
    {
        string ascii = Renderer.ToAscii(SingleDarkModule(), quietZone: 1, dark: "#", light: ".");
        string[] lines = ascii.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

        Assert.Equal(3, lines.Length); // 1 + 2*1
        Assert.Equal("...", lines[0]);
        Assert.Equal(".#.", lines[1]);
        Assert.Equal("...", lines[2]);
    }
}
