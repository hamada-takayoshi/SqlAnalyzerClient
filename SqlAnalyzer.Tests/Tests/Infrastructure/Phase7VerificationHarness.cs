using SqlAnalyzer.Domain.Model;
using SqlAnalyzer.SqlServer.Analysis;
using SqlAnalyzer.SqlServer.Boundary;

namespace SqlAnalyzer.Tests.Tests.Infrastructure;

internal static class Phase7VerificationHarness
{
    public static void VerifyOrThrow()
    {
        ISqlAnalyzer analyzer = new SqlServerAnalyzer();
        StatementBoundaryExtractor boundaryExtractor = new();

        // 1) MULTI_STATEMENT_TRUNCATED
        SqlAnalysisResult multi = AnalyzeWithBoundary(analyzer, boundaryExtractor, "SELECT 1; SELECT 2;");
        Expect(HasCode(multi, "MULTI_STATEMENT_TRUNCATED"), "MULTI_STATEMENT_TRUNCATED");

        // 2) DDL_NOT_SUPPORTED
        SqlAnalysisResult ddl = AnalyzeWithBoundary(analyzer, boundaryExtractor, "CREATE TABLE T (Id int);");
        Expect(HasCode(ddl, "DDL_NOT_SUPPORTED"), "DDL_NOT_SUPPORTED");

        // 3) PARTIAL_PARSE
        SqlAnalysisResult partial = AnalyzeWithBoundary(analyzer, boundaryExtractor, "SELECT FROM;");
        Expect(HasCode(partial, "PARTIAL_PARSE"), "PARTIAL_PARSE");

        // 4) UNSUPPORTED_SYNTAX
        SqlAnalysisResult unsupported = AnalyzeWithBoundary(analyzer, boundaryExtractor, "DECLARE @x int;");
        Expect(HasCode(unsupported, "UNSUPPORTED_SYNTAX"), "UNSUPPORTED_SYNTAX");

        // 5) Best-effort: partial parse still returns a model and no crash
        Expect(partial.Statement is not null, "Best-effort model returned");
    }

    private static SqlAnalysisResult AnalyzeWithBoundary(ISqlAnalyzer analyzer, StatementBoundaryExtractor extractor, string sql)
    {
        StatementBoundaryExtractionResult boundary = extractor.Extract(sql);
        SqlAnalysisResult result = analyzer.AnalyzeAsync(SqlDialect.SqlServer, boundary.NormalizedText, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        List<Diagnostic> diagnostics = result.Diagnostics.ToList();
        if (boundary.HasTrailingStatements && diagnostics.All(d => d.Code != "MULTI_STATEMENT_TRUNCATED"))
        {
            diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Code = "MULTI_STATEMENT_TRUNCATED",
                Message = "Only the first SQL statement was analyzed. Trailing statements were ignored."
            });
        }

        return result with
        {
            Document = new SqlDocumentInfo
            {
                Boundary = boundary.Boundary,
                HasTrailingStatements = boundary.HasTrailingStatements
            },
            Diagnostics = diagnostics
        };
    }

    private static bool HasCode(SqlAnalysisResult result, string code)
    {
        return result.Diagnostics.Any(d => string.Equals(d.Code, code, StringComparison.Ordinal));
    }

    private static void Expect(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Phase7 verification failed: {name}");
        }
    }
}
