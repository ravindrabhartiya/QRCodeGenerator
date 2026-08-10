namespace QrGen.Tests;

/// <summary>
/// Smoke test proving the test harness is wired up and runnable.
/// Real behavior tests are added per increment following TDD.
/// </summary>
public class HarnessTests
{
    /// <summary>Verifies the xUnit runner executes and assertions work.</summary>
    [Fact]
    public void Harness_IsWired()
    {
        Assert.True(true);
    }
}
