using SqlAnalyzer.Domain.Model;

namespace SqlAnalyzer.SqlServer.Boundary;

public sealed record StatementBoundaryExtractionResult
{
    public StatementBoundary Boundary { get; init; } = new();

    public string NormalizedText { get; init; } = string.Empty;

    public bool HasTrailingStatements { get; init; }
}
