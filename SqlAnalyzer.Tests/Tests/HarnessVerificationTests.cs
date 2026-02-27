using SqlAnalyzer.Tests.Tests.Infrastructure;

namespace SqlAnalyzer.Tests.Tests;

public class HarnessVerificationTests
{
    [Fact]
    public void BoundaryHarness_Passes()
    {
        BoundaryExtractionHarness.VerifyOrThrow();
    }

    [Fact]
    public void Phase5Harness_Passes()
    {
        Phase5VerificationHarness.VerifyOrThrow();
    }

    [Fact]
    public void Phase6Harness_Passes()
    {
        Phase6VerificationHarness.VerifyOrThrow();
    }

    [Fact]
    public void Phase7Harness_Passes()
    {
        Phase7VerificationHarness.VerifyOrThrow();
    }

    [Fact]
    public void Phase8Harness_Passes()
    {
        Phase8VerificationHarness.VerifyOrThrow();
    }
}
