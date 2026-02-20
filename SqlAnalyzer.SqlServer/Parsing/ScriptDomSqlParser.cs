using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlAnalyzer.SqlServer.Parsing;

internal sealed class ScriptDomSqlParser
{
    public ScriptDomParseResult Parse(string sqlText)
    {
        using StringReader reader = new(sqlText);
        TSql160Parser parser = new(initialQuotedIdentifiers: true);

        IList<ParseError> errors;
        TSqlFragment? fragment = parser.Parse(reader, out errors);
        TSqlStatement? firstStatement = GetFirstStatement(fragment);

        return new ScriptDomParseResult
        {
            Fragment = fragment,
            FirstStatement = firstStatement,
            Errors = errors.ToList()
        };
    }

    private static TSqlStatement? GetFirstStatement(TSqlFragment? fragment)
    {
        if (fragment is not TSqlScript script || script.Batches.Count == 0)
        {
            return null;
        }

        TSqlBatch firstBatch = script.Batches[0];
        if (firstBatch.Statements.Count == 0)
        {
            return null;
        }

        return firstBatch.Statements[0];
    }
}
