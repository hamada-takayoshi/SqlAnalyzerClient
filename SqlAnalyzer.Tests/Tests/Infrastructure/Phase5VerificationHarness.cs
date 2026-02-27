using SqlAnalyzer.Domain.Model;
using SqlAnalyzer.SqlServer.Analysis;
using SqlAnalyzer.SqlServer.Boundary;

namespace SqlAnalyzer.Tests.Tests.Infrastructure;

internal static class Phase5VerificationHarness
{
    public static void VerifyOrThrow()
    {
        ISqlAnalyzer analyzer = new SqlServerAnalyzer();
        StatementBoundaryExtractor boundaryExtractor = new();

        // Case 1
        SqlAnalysisResult case1 = AnalyzeWithBoundary(analyzer, boundaryExtractor, """
SELECT A.Id AS UserId
FROM Users A;
""");
        SelectStatement case1Select = GetSelectStatement(case1, "Case1");
        SelectItem case1Item = case1Select.SelectItems.First();
        Expect(case1Item.OutputName == "UserId", "Case1 OutputName");
        Expect(case1Item.ExpressionText.Trim() == "A.Id", "Case1 ExpressionText");
        Expect(case1Item.SourceColumn?.TableAliasOrName == "Users", "Case1 SourceTable");

        // Case 2
        SqlAnalysisResult case2 = AnalyzeWithBoundary(analyzer, boundaryExtractor, """
SELECT A.Name /* User Name */
FROM Users A;
""");
        SelectStatement case2Select = GetSelectStatement(case2, "Case2");
        SelectItem case2Item = case2Select.SelectItems.First();
        Expect(case2Item.LogicalName == "User Name", "Case2 LogicalName");

        // Case 3
        SqlAnalysisResult case3 = AnalyzeWithBoundary(analyzer, boundaryExtractor, """
SELECT A.Name -- User Name
FROM Users A;
""");
        SelectStatement case3Select = GetSelectStatement(case3, "Case3");
        SelectItem case3Item = case3Select.SelectItems.First();
        Expect(case3Item.LogicalName == "User Name", "Case3 LogicalName");

        // Case 4
        SqlAnalysisResult case4 = AnalyzeWithBoundary(analyzer, boundaryExtractor, """
SELECT COUNT(*) TotalCount
FROM Users;
""");
        SelectStatement case4Select = GetSelectStatement(case4, "Case4");
        SelectItem case4Item = case4Select.SelectItems.First();
        Expect(case4Item.OutputName == "TotalCount", "Case4 OutputName");
        Expect(case4Item.SourceColumn is null, "Case4 SourceTable null");

        // Case 5
        SqlAnalysisResult case5 = AnalyzeWithBoundary(analyzer, boundaryExtractor, "UPDATE Users SET Name = 'X';");
        Expect(case5.Statement.StatementType == SqlStatementType.Update, "Case5 StatementType");
        Expect(case5.Statement is not SelectStatement, "Case5 NotSelect");
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

    private static SelectStatement GetSelectStatement(SqlAnalysisResult result, string caseName)
    {
        if (result.Statement is SelectStatement selectStatement)
        {
            return selectStatement;
        }

        throw new InvalidOperationException($"Phase5 verification failed: {caseName} is not Select.");
    }

    private static void Expect(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Phase5 verification failed: {name}");
        }
    }
}
