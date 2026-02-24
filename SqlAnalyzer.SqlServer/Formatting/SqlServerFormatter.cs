using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlAnalyzer.Domain.Model;

namespace SqlAnalyzer.SqlServer.Formatting;

public sealed class SqlServerFormatter : ISqlFormatter
{
    public Task<SqlFormatResult> FormatAsync(SqlDialect dialect, string sqlText, SqlFormatOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (dialect != SqlDialect.SqlServer)
        {
            return Task.FromResult(new SqlFormatResult
            {
                FormattedSql = sqlText,
                Diagnostics =
                [
                    new Diagnostic
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Code = "UNSUPPORTED_SYNTAX",
                        Message = $"Formatter does not support dialect '{dialect}'."
                    }
                ]
            });
        }

        string safeSql = sqlText ?? string.Empty;
        using StringReader reader = new(safeSql);
        TSql160Parser parser = new(initialQuotedIdentifiers: true);

        IList<ParseError> parseErrors;
        TSqlFragment? fragment = parser.Parse(reader, out parseErrors);

        if (fragment is null || parseErrors.Count > 0)
        {
            return Task.FromResult(new SqlFormatResult
            {
                FormattedSql = safeSql,
                Diagnostics = BuildParseDiagnostics(parseErrors.ToList())
            });
        }

        try
        {
            Sql160ScriptGenerator generator = new(new SqlScriptGeneratorOptions
            {
                IndentationSize = Math.Max(1, options.IndentationWidth),
                KeywordCasing = options.UppercaseKeywords ? KeywordCasing.Uppercase : KeywordCasing.Lowercase,
                IncludeSemicolons = true
            });

            generator.GenerateScript(fragment, out string formatted);

            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new SqlFormatResult
            {
                FormattedSql = formatted
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new SqlFormatResult
            {
                FormattedSql = safeSql,
                Diagnostics =
                [
                    new Diagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Code = "UNSUPPORTED_SYNTAX",
                        Message = $"Formatting failed: {ex.Message}"
                    }
                ]
            });
        }
    }

    private static IReadOnlyList<Diagnostic> BuildParseDiagnostics(IReadOnlyList<ParseError> errors)
    {
        if (errors.Count == 0)
        {
            return
            [
                new Diagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Code = "UNSUPPORTED_SYNTAX",
                    Message = "Formatting failed due to unsupported syntax."
                }
            ];
        }

        ParseError first = errors[0];
        return
        [
            new Diagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Code = "PARTIAL_PARSE",
                Message = $"Formatting parse error ({errors.Count}): {first.Message}",
                Span = new SourceSpan
                {
                    StartIndex = first.Offset,
                    Length = 1,
                    StartLine = first.Line,
                    StartColumn = first.Column
                }
            }
        ];
    }
}
