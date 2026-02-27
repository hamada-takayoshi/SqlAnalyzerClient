using SqlAnalyzer.App.Services;
using SqlAnalyzer.Domain.Model;
using SqlAnalyzer.SqlServer.Analysis;
using SqlAnalyzer.SqlServer.Boundary;

namespace SqlAnalyzer.Tests.Tests.Infrastructure;

internal static class Phase6VerificationHarness
{
    public static void VerifyOrThrow()
    {
        ISqlAnalyzer analyzer = new SqlServerAnalyzer();
        StatementBoundaryExtractor boundaryExtractor = new();
        DiagramService diagramService = new();

        // 1) Simple JOIN
        SqlAnalysisResult case1 = AnalyzeWithBoundary(analyzer, boundaryExtractor, """
SELECT *
FROM A
LEFT JOIN B ON A.Id = B.AId;
""");
        DiagramArtifacts diagram1 = diagramService.Generate(case1.Statement);
        Expect(diagram1.MermaidText.Contains("flowchart LR", StringComparison.Ordinal), "Case1 Mermaid header");
        Expect(diagram1.MermaidText.Contains("|LeftOuter|", StringComparison.Ordinal), "Case1 JoinType label");
        Expect(CountOccurrences(diagram1.MermaidText, "-->") == 1, "Case1 edge count");
        Expect(diagram1.PngBytes is { Length: > 0 }, "Case1 PNG render");

        // 2) APPLY
        SqlAnalysisResult case2 = AnalyzeWithBoundary(analyzer, boundaryExtractor, """
SELECT *
FROM A
OUTER APPLY dbo.FN(A.Id) F;
""");
        DiagramArtifacts diagram2 = diagramService.Generate(case2.Statement);
        Expect(diagram2.MermaidText.Contains("|OuterApply|", StringComparison.Ordinal), "Case2 Apply label");
        Expect(diagram2.PngBytes is { Length: > 0 }, "Case2 PNG render");

        // 3) No relations
        SqlAnalysisResult case3 = AnalyzeWithBoundary(analyzer, boundaryExtractor, """
SELECT *
FROM SingleTable;
""");
        DiagramArtifacts diagram3 = diagramService.Generate(case3.Statement);
        Expect(diagram3.MermaidText.Contains("flowchart LR", StringComparison.Ordinal), "Case3 Mermaid header");
        Expect(CountOccurrences(diagram3.MermaidText, "-->") == 0, "Case3 no edges");
        Expect(diagram3.PngBytes is { Length: > 0 }, "Case3 PNG render");

        // 4) Multiple relations + stable ordering
        SqlAnalysisResult case4 = AnalyzeWithBoundary(analyzer, boundaryExtractor, """
SELECT *
FROM A
INNER JOIN B ON A.Id = B.AId
LEFT JOIN C ON B.Id = C.BId;
""");
        DiagramArtifacts diagram4a = diagramService.Generate(case4.Statement);
        DiagramArtifacts diagram4b = diagramService.Generate(case4.Statement);
        Expect(string.Equals(diagram4a.MermaidText, diagram4b.MermaidText, StringComparison.Ordinal), "Case4 deterministic Mermaid");
        Expect(diagram4a.PngBytes is { Length: > 0 }, "Case4 PNG render");
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

    private static int CountOccurrences(string text, string token)
    {
        int count = 0;
        int index = 0;
        while (true)
        {
            index = text.IndexOf(token, index, StringComparison.Ordinal);
            if (index < 0)
            {
                return count;
            }

            count++;
            index += token.Length;
        }
    }

    private static void Expect(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Phase6 verification failed: {name}");
        }
    }
}
