using SqlAnalyzer.Domain.Model;

namespace SqlAnalyzer.SqlServer.Analysis;

public interface ISqlAnalyzer
{
    Task<SqlAnalysisResult> AnalyzeAsync(SqlDialect dialect, string sqlText, CancellationToken cancellationToken);
}
