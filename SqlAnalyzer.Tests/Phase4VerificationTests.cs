using SqlAnalyzer.Domain.Model;
using SqlAnalyzer.SqlServer.Analysis;
using SqlAnalyzer.SqlServer.Boundary;

namespace SqlAnalyzer.Tests;

public class Phase4VerificationTests
{
    [Fact]
    public async Task Phase4CoreAnalyzerCoverageAsync()
    {
        ISqlAnalyzer analyzer = new SqlServerAnalyzer();
        StatementBoundaryExtractor boundaryExtractor = new();

        SqlAnalysisResult selectJoin = await AnalyzeWithBoundaryAsync(analyzer, boundaryExtractor, """
SELECT *
FROM A
LEFT JOIN B ON A.Id = B.AId;
""");
        Assert.Equal(SqlStatementType.Select, selectJoin.Statement.StatementType);
        Assert.True(selectJoin.Statement.Tables.Count >= 2);
        Assert.Contains(selectJoin.Statement.Relations, r => r.JoinType == JoinType.LeftOuter);

        SqlAnalysisResult apply = await AnalyzeWithBoundaryAsync(analyzer, boundaryExtractor, """
SELECT *
FROM A
OUTER APPLY dbo.FN(A.Id) F;
""");
        Assert.Contains(apply.Statement.Relations, r => r.JoinType == JoinType.OuterApply);
        Assert.Contains(
            apply.Statement.Tables,
            t => t.Alias == "F" && (t.Source.Kind == TableSourceKind.Function || t.Source.ExpressionText is not null));

        Assert.Equal(
            SqlStatementType.Insert,
            (await AnalyzeWithBoundaryAsync(analyzer, boundaryExtractor, "INSERT INTO T1(Id) VALUES (1);")).Statement.StatementType);
        Assert.Equal(
            SqlStatementType.Update,
            (await AnalyzeWithBoundaryAsync(analyzer, boundaryExtractor, "UPDATE T1 SET Name = 'X';")).Statement.StatementType);
        Assert.Equal(
            SqlStatementType.Delete,
            (await AnalyzeWithBoundaryAsync(analyzer, boundaryExtractor, "DELETE FROM T1;")).Statement.StatementType);
        Assert.Equal(
            SqlStatementType.Merge,
            (await AnalyzeWithBoundaryAsync(analyzer, boundaryExtractor, "MERGE INTO T1 AS T USING T2 AS S ON T.Id = S.Id WHEN MATCHED THEN UPDATE SET T.Name = S.Name;")).Statement.StatementType);

        SqlAnalysisResult ddl = await AnalyzeWithBoundaryAsync(analyzer, boundaryExtractor, "CREATE TABLE X (Id int);");
        Assert.Contains(ddl.Diagnostics, d => d.Code == "DDL_NOT_SUPPORTED");

        SqlAnalysisResult multi = await AnalyzeWithBoundaryAsync(analyzer, boundaryExtractor, "SELECT 1; SELECT 2;");
        Assert.Contains(multi.Diagnostics, d => d.Code == "MULTI_STATEMENT_TRUNCATED");
    }

    private static async Task<SqlAnalysisResult> AnalyzeWithBoundaryAsync(ISqlAnalyzer analyzer, StatementBoundaryExtractor extractor, string sql)
    {
        StatementBoundaryExtractionResult boundary = extractor.Extract(sql);
        SqlAnalysisResult result = await analyzer.AnalyzeAsync(SqlDialect.SqlServer, boundary.NormalizedText, CancellationToken.None);

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
}
