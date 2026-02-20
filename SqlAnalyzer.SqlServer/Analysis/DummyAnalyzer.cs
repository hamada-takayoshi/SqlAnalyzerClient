using SqlAnalyzer.Domain.Model;

namespace SqlAnalyzer.SqlServer.Analysis;

public sealed class DummyAnalyzer : ISqlAnalyzer
{
    public async Task<SqlAnalysisResult> AnalyzeAsync(SqlDialect dialect, string sqlText, CancellationToken cancellationToken)
    {
        await Task.Delay(800, cancellationToken);

        TableRef tableOrders = new()
        {
            Id = new TableRefId("t1"),
            Alias = "o",
            LogicalName = "Orders",
            Source = new TableSourceRef
            {
                Kind = TableSourceKind.PhysicalTable,
                Name = new QualifiedName
                {
                    Database = "SalesDb",
                    Schema = "dbo",
                    Object = "Orders",
                    Raw = "[dbo].[Orders]"
                }
            }
        };

        TableRef tableCustomers = new()
        {
            Id = new TableRefId("t2"),
            Alias = "c",
            LogicalName = "Customers",
            Source = new TableSourceRef
            {
                Kind = TableSourceKind.PhysicalTable,
                Name = new QualifiedName
                {
                    Database = "SalesDb",
                    Schema = "dbo",
                    Object = "Customers",
                    Raw = "[dbo].[Customers]"
                }
            }
        };

        SelectStatement statement = new()
        {
            Tables = new[] { tableOrders, tableCustomers },
            Relations = new[]
            {
                new TableRelation
                {
                    From = new TableRefId("t1"),
                    To = new TableRefId("t2"),
                    JoinType = JoinType.LeftOuter,
                    ConditionText = "o.CustomerId = c.CustomerId"
                }
            },
            SelectItems = new[]
            {
                new SelectItem
                {
                    OutputName = "OrderId",
                    ExpressionText = "o.OrderId",
                    SourceColumn = new ColumnRef
                    {
                        ColumnName = "OrderId",
                        TableAliasOrName = "o",
                        ResolvedTable = new TableRefId("t1")
                    },
                    LogicalName = "Order Identifier"
                },
                new SelectItem
                {
                    OutputName = "CustomerName",
                    ExpressionText = "c.CustomerName",
                    SourceColumn = new ColumnRef
                    {
                        ColumnName = "CustomerName",
                        TableAliasOrName = "c",
                        ResolvedTable = new TableRefId("t2")
                    },
                    LogicalName = "Customer Name"
                }
            },
            Span = new SourceSpan
            {
                StartIndex = 0,
                Length = Math.Max(sqlText.Length, 1),
                StartLine = 1,
                StartColumn = 1
            }
        };

        return new SqlAnalysisResult
        {
            Dialect = dialect,
            Document = new SqlDocumentInfo
            {
                Boundary = new StatementBoundary
                {
                    StartIndex = 0,
                    EndIndexExclusive = sqlText.Length,
                    Kind = BoundaryKind.EndOfText
                },
                HasTrailingStatements = false
            },
            Statement = statement,
            Diagnostics = new[]
            {
                new Diagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Code = "PARTIAL_PARSE",
                    Message = "Dummy analyzer result for current phase."
                }
            }
        };
    }
}