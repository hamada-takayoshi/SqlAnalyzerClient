using SqlAnalyzer.Domain.Model;

namespace SqlAnalyzer.SqlServer.Formatting;

public sealed record SqlFormatResult
{
    public string FormattedSql { get; init; } = string.Empty;

    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();
}
