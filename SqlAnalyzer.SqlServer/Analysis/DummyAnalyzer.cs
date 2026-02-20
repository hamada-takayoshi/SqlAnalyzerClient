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
            LogicalName = "受注",
            Source = new TableSourceRef
            {
                Kind = TableSourceKind.PhysicalTable,
                Name = new QualifiedName
                {
                    Database = "SalesDb",
                    Schema = "dbo",
                    Object = "Orders",
                    Raw = "[dbo].[Orders]"
                },
                Span = new SourceSpan
                {
                    StartIndex = 14,
                    Length = 12,
                    StartLine = 2,
                    StartColumn = 6
                }
            },
            Span = new SourceSpan
            {
                StartIndex = 14,
                Length = 14,
                StartLine = 2,
                StartColumn = 6
            }
        };

        TableRef tableCustomers = new()
        {
            Id = new TableRefId("t2"),
            Alias = "c",
            LogicalName = "顧客",
            Source = new TableSourceRef
            {
                Kind = TableSourceKind.PhysicalTable,
                Name = new QualifiedName
                {
                    Database = "SalesDb",
                    Schema = "dbo",
                    Object = "Customers",
                    Raw = "[dbo].[Customers]"
                },
                Span = new SourceSpan
                {
                    StartIndex = 42,
                    Length = 15,
                    StartLine = 3,
                    StartColumn = 11
                }
            },
            Span = new SourceSpan
            {
                StartIndex = 42,
                Length = 17,
                StartLine = 3,
                StartColumn = 11
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
                    ConditionText = "o.CustomerId = c.CustomerId",
                    Span = new SourceSpan
                    {
                        StartIndex = 34,
                        Length = 42,
                        StartLine = 3,
                        StartColumn = 1
                    }
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
                        ResolvedTable = new TableRefId("t1"),
                        Span = new SourceSpan
                        {
                            StartIndex = 7,
                            Length = 9,
                            StartLine = 1,
                            StartColumn = 8
                        }
                    },
                    LogicalName = "受注ID",
                    Span = new SourceSpan
                    {
                        StartIndex = 7,
                        Length = 9,
                        StartLine = 1,
                        StartColumn = 8
                    }
                },
                new SelectItem
                {
                    OutputName = "CustomerName",
                    ExpressionText = "c.CustomerName",
                    SourceColumn = new ColumnRef
                    {
                        ColumnName = "CustomerName",
                        TableAliasOrName = "c",
                        ResolvedTable = new TableRefId("t2"),
                        Span = new SourceSpan
                        {
                            StartIndex = 18,
                            Length = 14,
                            StartLine = 1,
                            StartColumn = 19
                        }
                    },
                    LogicalName = "顧客名",
                    Span = new SourceSpan
                    {
                        StartIndex = 18,
                        Length = 14,
                        StartLine = 1,
                        StartColumn = 19
                    }
                }
            },
            Span = new SourceSpan
            {
                StartIndex = 0,
                Length = Math.Max(sqlText?.Length ?? 0, 80),
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
                    EndIndexExclusive = Math.Max(sqlText?.Length ?? 0, 80),
                    Kind = BoundaryKind.Semicolon
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
                    Message = "Dummy analyzer result for Phase 2.",
                    Span = new SourceSpan
                    {
                        StartIndex = 0,
                        Length = 6,
                        StartLine = 1,
                        StartColumn = 1
                    }
                }
            }
        };
    }
}
