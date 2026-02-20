using SqlAnalyzer.Domain.Model;
using SqlAnalyzer.SqlServer.Analysis;
using SqlAnalyzer.SqlServer.Boundary;

namespace SqlAnalyzer.App.Verification;

internal static class Phase4VerificationHarness
{
    public static void VerifyOrThrow()
    {
        ISqlAnalyzer analyzer = new SqlServerAnalyzer();
        StatementBoundaryExtractor boundaryExtractor = new();

        // 1) SELECT with JOIN
        SqlAnalysisResult selectJoin = AnalyzeWithBoundary(analyzer, boundaryExtractor, """
SELECT *
FROM A
LEFT JOIN B ON A.Id = B.AId;
""");
        Expect(selectJoin.Statement.StatementType == SqlStatementType.Select, "SELECT JOIN statement type");
        Expect(selectJoin.Statement.Tables.Count >= 2, "SELECT JOIN tables");
        Expect(selectJoin.Statement.Relations.Any(r => r.JoinType == JoinType.LeftOuter), "SELECT JOIN relation");

        // 2) APPLY
        SqlAnalysisResult apply = AnalyzeWithBoundary(analyzer, boundaryExtractor, """
SELECT *
FROM A
OUTER APPLY dbo.FN(A.Id) F;
""");
        Expect(apply.Statement.Relations.Any(r => r.JoinType == JoinType.OuterApply), "OUTER APPLY relation");
        Expect(apply.Statement.Tables.Any(t => t.Alias == "F" && (t.Source.Kind == TableSourceKind.Function || t.Source.ExpressionText is not null)),
            "OUTER APPLY function table");

        // 3) INSERT / UPDATE / DELETE / MERGE recognition
        Expect(AnalyzeWithBoundary(analyzer, boundaryExtractor, "INSERT INTO T1(Id) VALUES (1);").Statement.StatementType == SqlStatementType.Insert, "INSERT type");
        Expect(AnalyzeWithBoundary(analyzer, boundaryExtractor, "UPDATE T1 SET Name = 'X';").Statement.StatementType == SqlStatementType.Update, "UPDATE type");
        Expect(AnalyzeWithBoundary(analyzer, boundaryExtractor, "DELETE FROM T1;").Statement.StatementType == SqlStatementType.Delete, "DELETE type");
        Expect(AnalyzeWithBoundary(analyzer, boundaryExtractor, "MERGE INTO T1 AS T USING T2 AS S ON T.Id = S.Id WHEN MATCHED THEN UPDATE SET T.Name = S.Name;").Statement.StatementType == SqlStatementType.Merge, "MERGE type");

        // 4) DDL detection
        SqlAnalysisResult ddl = AnalyzeWithBoundary(analyzer, boundaryExtractor, "CREATE TABLE X (Id int);");
        Expect(ddl.Diagnostics.Any(d => d.Code == "DDL_NOT_SUPPORTED"), "DDL diagnostic");

        // 5) Multi-statement truncation persists
        SqlAnalysisResult multi = AnalyzeWithBoundary(analyzer, boundaryExtractor, "SELECT 1; SELECT 2;");
        Expect(multi.Diagnostics.Any(d => d.Code == "MULTI_STATEMENT_TRUNCATED"), "Multi statement diagnostic");
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

    private static void Expect(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Phase 4 verification failed: {name}");
        }
    }
}
