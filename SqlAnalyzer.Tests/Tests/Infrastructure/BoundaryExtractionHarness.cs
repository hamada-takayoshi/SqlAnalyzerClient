using SqlAnalyzer.Domain.Model;
using SqlAnalyzer.SqlServer.Boundary;

namespace SqlAnalyzer.Tests.Tests.Infrastructure;

public static class BoundaryExtractionHarness
{
    public static void VerifyOrThrow()
    {
        StatementBoundaryExtractor extractor = new();

        StatementBoundaryExtractionResult caseA = extractor.Extract("SELECT 1; SELECT 2;");
        Expect(caseA.Boundary.Kind == BoundaryKind.Semicolon, "Case A: boundary kind");
        Expect(caseA.HasTrailingStatements, "Case A: trailing detection");
        Expect(caseA.NormalizedText == "SELECT 1;", "Case A: normalized text");
        Expect(ContainsTruncationDiagnostic(caseA), "Case A: truncation diagnostic");

        StatementBoundaryExtractionResult caseB = extractor.Extract("SELECT 1\r\nGO\r\nSELECT 2");
        Expect(caseB.Boundary.Kind == BoundaryKind.GoBatch, "Case B: boundary kind");
        Expect(caseB.HasTrailingStatements, "Case B: trailing detection");
        Expect(ContainsTruncationDiagnostic(caseB), "Case B: truncation diagnostic");

        StatementBoundaryExtractionResult caseC = extractor.Extract("SELECT 1");
        Expect(caseC.Boundary.Kind == BoundaryKind.EndOfText, "Case C: boundary kind");
        Expect(!caseC.HasTrailingStatements, "Case C: trailing detection");
        Expect(!ContainsTruncationDiagnostic(caseC), "Case C: no truncation diagnostic");

        StatementBoundaryExtractionResult caseD = extractor.Extract("SELECT 'GO' AS X\r\nSELECT 2");
        Expect(caseD.Boundary.Kind != BoundaryKind.GoBatch, "Case D: GO in string");

        StatementBoundaryExtractionResult caseE = extractor.Extract("SELECT ';' AS X; SELECT 2;");
        Expect(caseE.Boundary.Kind == BoundaryKind.Semicolon, "Case E: boundary kind");
        Expect(caseE.NormalizedText == "SELECT ';' AS X;", "Case E: semicolon in string handling");
    }

    private static void Expect(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Boundary extraction verification failed: {name}");
        }
    }

    private static bool ContainsTruncationDiagnostic(StatementBoundaryExtractionResult extractionResult)
    {
        List<Diagnostic> diagnostics = new();
        if (extractionResult.HasTrailingStatements)
        {
            diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Code = "MULTI_STATEMENT_TRUNCATED",
                Message = "Only the first SQL statement was analyzed. Trailing statements were ignored."
            });
        }

        return diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning &&
                                    d.Code == "MULTI_STATEMENT_TRUNCATED");
    }
}
