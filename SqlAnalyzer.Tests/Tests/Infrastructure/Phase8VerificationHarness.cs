using SqlAnalyzer.Domain.Model;
using SqlAnalyzer.SqlServer.Formatting;

namespace SqlAnalyzer.Tests.Tests.Infrastructure;

internal static class Phase8VerificationHarness
{
    public static void VerifyOrThrow()
    {
        ISqlFormatter formatter = new SqlServerFormatter();

        // 1) Uppercase + indentation
        SqlFormatResult upper = formatter.FormatAsync(
                SqlDialect.SqlServer,
                "select a.id from users a where a.id=1",
                new SqlFormatOptions
                {
                    IndentationWidth = 4,
                    UppercaseKeywords = true
                },
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        Expect(upper.Diagnostics.Count == 0, "Uppercase formatting diagnostics");
        Expect(upper.FormattedSql.Contains("SELECT", StringComparison.Ordinal), "Uppercase formatting keyword");

        // 2) Lowercase option
        SqlFormatResult lower = formatter.FormatAsync(
                SqlDialect.SqlServer,
                "SELECT A.ID FROM USERS A",
                new SqlFormatOptions
                {
                    IndentationWidth = 2,
                    UppercaseKeywords = false
                },
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        Expect(lower.Diagnostics.Count == 0, "Lowercase formatting diagnostics");
        Expect(lower.FormattedSql.Contains("select", StringComparison.Ordinal), "Lowercase formatting keyword");

        // 3) Invalid SQL -> diagnostics, no crash
        SqlFormatResult invalid = formatter.FormatAsync(
                SqlDialect.SqlServer,
                "SELECT FROM",
                new SqlFormatOptions(),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        Expect(invalid.Diagnostics.Count > 0, "Invalid SQL diagnostics");
        Expect(invalid.Diagnostics.Any(d => d.Code is "PARTIAL_PARSE" or "UNSUPPORTED_SYNTAX"), "Invalid SQL diagnostics code");
    }

    private static void Expect(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Phase8 verification failed: {name}");
        }
    }
}
