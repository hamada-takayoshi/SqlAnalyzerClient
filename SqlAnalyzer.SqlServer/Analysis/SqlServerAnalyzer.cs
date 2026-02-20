using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlAnalyzer.Domain.Model;
using SqlAnalyzer.SqlServer.Parsing;

namespace SqlAnalyzer.SqlServer.Analysis;

public sealed class SqlServerAnalyzer : ISqlAnalyzer
{
    private readonly ScriptDomSqlParser _parser = new();

    public Task<SqlAnalysisResult> AnalyzeAsync(SqlDialect dialect, string sqlText, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ScriptDomParseResult parseResult = _parser.Parse(sqlText ?? string.Empty);
        List<Diagnostic> diagnostics = [];
        AddParseDiagnostics(parseResult.Errors, diagnostics);

        SqlStatement statement = new UnknownStatement();

        if (parseResult.FirstStatement is null)
        {
            diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Code = "UNSUPPORTED_SYNTAX",
                Message = "Could not recognize a SQL statement from input."
            });
        }
        else
        {
            SqlStatementDomainMapper mapper = new(sqlText ?? string.Empty);
            if (mapper.IsDdlStatement(parseResult.FirstStatement))
            {
                diagnostics.Add(new Diagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Code = "DDL_NOT_SUPPORTED",
                    Message = "DDL is not supported in this version."
                });
                statement = new UnknownStatement();
            }
            else
            {
                statement = mapper.MapStatement(parseResult.FirstStatement);
                if (statement.StatementType == SqlStatementType.Unknown)
                {
                    diagnostics.Add(new Diagnostic
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Code = "UNSUPPORTED_SYNTAX",
                        Message = $"Statement type '{parseResult.FirstStatement.GetType().Name}' is not supported yet."
                    });
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        SqlAnalysisResult result = new()
        {
            Dialect = SqlDialect.SqlServer,
            Document = new SqlDocumentInfo
            {
                Boundary = new StatementBoundary
                {
                    StartIndex = 0,
                    EndIndexExclusive = sqlText?.Length ?? 0,
                    Kind = BoundaryKind.EndOfText
                },
                HasTrailingStatements = false
            },
            Statement = statement,
            Diagnostics = diagnostics
        };

        return Task.FromResult(result);
    }

    private static void AddParseDiagnostics(IReadOnlyList<ParseError> errors, ICollection<Diagnostic> diagnostics)
    {
        if (errors.Count == 0)
        {
            return;
        }

        ParseError first = errors[0];
        diagnostics.Add(new Diagnostic
        {
            Severity = DiagnosticSeverity.Warning,
            Code = "PARTIAL_PARSE",
            Message = $"Parser reported {errors.Count} issue(s). First: {first.Message}",
            Span = new SourceSpan
            {
                StartIndex = first.Offset,
                Length = 1,
                StartLine = first.Line,
                StartColumn = first.Column
            }
        });
    }
}
