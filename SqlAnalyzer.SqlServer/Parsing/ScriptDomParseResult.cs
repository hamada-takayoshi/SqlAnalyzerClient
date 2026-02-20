using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlAnalyzer.SqlServer.Parsing;

internal sealed record ScriptDomParseResult
{
    public TSqlFragment? Fragment { get; init; }

    public TSqlStatement? FirstStatement { get; init; }

    public IReadOnlyList<ParseError> Errors { get; init; } = Array.Empty<ParseError>();
}
