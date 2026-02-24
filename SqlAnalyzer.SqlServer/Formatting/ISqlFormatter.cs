using SqlAnalyzer.Domain.Model;

namespace SqlAnalyzer.SqlServer.Formatting;

public interface ISqlFormatter
{
    Task<SqlFormatResult> FormatAsync(SqlDialect dialect, string sqlText, SqlFormatOptions options, CancellationToken cancellationToken);
}
