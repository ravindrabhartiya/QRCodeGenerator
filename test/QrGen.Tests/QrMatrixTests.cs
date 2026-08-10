using System;
using Xunit;

namespace QrGen.Tests;

/// <summary>Unit tests for <see cref="QrMatrix"/>.</summary>
public class QrMatrixTests
{
    [Fact]
    public void NewMatrix_IsAllLightAndNonFunction()
    {
        var m = new QrMatrix(33);
        Assert.Equal(33, m.Size);
        for (int r = 0; r < 33; r++)
        {
            for (int c = 0; c < 33; c++)
            {
                Assert.False(m.IsDark(r, c));
                Assert.False(m.IsFunction(r, c));
            }
        }

        Assert.Equal(0, m.CountFunction());
        Assert.Equal(33 * 33, m.CountData());
    }

    [Fact]
    public void SetFunction_MarksDarkAndReserved()
    {
        var m = new QrMatrix(33);
        m.SetFunction(1, 2, true);
        Assert.True(m.IsDark(1, 2));
        Assert.True(m.IsFunction(1, 2));
        Assert.Equal(1, m.CountFunction());
    }

    [Fact]
    public void SetData_SetsColourButNotFunction()
    {
        var m = new QrMatrix(33);
        m.SetData(3, 4, true);
        Assert.True(m.IsDark(3, 4));
        Assert.False(m.IsFunction(3, 4));
    }

    [Fact]
    public void Constructor_RejectsNonPositiveSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new QrMatrix(0));
    }
}
