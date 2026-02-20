namespace SqlAnalyzer.Domain.Model;

public enum SqlDialect
{
    SqlServer = 0,
    Unknown = 999
}

public sealed record SqlAnalysisResult
{
    public SqlDialect Dialect { get; init; } = SqlDialect.Unknown;

    public SqlDocumentInfo Document { get; init; } = new();

    public SqlStatement Statement { get; init; } = new UnknownStatement();

    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();
}

public sealed record SqlDocumentInfo
{
    public StatementBoundary Boundary { get; init; } = new();

    public bool HasTrailingStatements { get; init; }
}

public sealed record StatementBoundary
{
    public int StartIndex { get; init; }

    public int EndIndexExclusive { get; init; }

    public BoundaryKind Kind { get; init; } = BoundaryKind.Unknown;
}

public enum BoundaryKind
{
    Semicolon = 0,
    GoBatch = 1,
    EndOfText = 2,
    Unknown = 999
}

public enum SqlStatementType
{
    Select = 0,
    Insert = 1,
    Update = 2,
    Delete = 3,
    Merge = 4,
    Unknown = 999
}

public abstract record SqlStatement
{
    protected SqlStatement(SqlStatementType statementType)
    {
        StatementType = statementType;
    }

    public SqlStatementType StatementType { get; init; }

    public IReadOnlyList<TableRef> Tables { get; init; } = Array.Empty<TableRef>();

    public IReadOnlyList<TableRelation> Relations { get; init; } = Array.Empty<TableRelation>();

    public SourceSpan? Span { get; init; }
}

public sealed record SelectStatement : SqlStatement
{
    public SelectStatement()
        : base(SqlStatementType.Select)
    {
    }

    public IReadOnlyList<SelectItem> SelectItems { get; init; } = Array.Empty<SelectItem>();
}

public enum InsertSourceKind
{
    Values = 0,
    Select = 1,
    DefaultValues = 2,
    Unknown = 999
}

public sealed record InsertStatement : SqlStatement
{
    public InsertStatement()
        : base(SqlStatementType.Insert)
    {
    }

    public TableRefId Target { get; init; } = new("t1");

    public InsertSourceKind SourceKind { get; init; } = InsertSourceKind.Unknown;

    public IReadOnlyList<string>? TargetColumns { get; init; }
}

public sealed record UpdateStatement : SqlStatement
{
    public UpdateStatement()
        : base(SqlStatementType.Update)
    {
    }

    public TableRefId Target { get; init; } = new("t1");

    public IReadOnlyList<string>? SetColumns { get; init; }
}

public sealed record DeleteStatement : SqlStatement
{
    public DeleteStatement()
        : base(SqlStatementType.Delete)
    {
    }

    public TableRefId Target { get; init; } = new("t1");
}

public sealed record MergeStatement : SqlStatement
{
    public MergeStatement()
        : base(SqlStatementType.Merge)
    {
    }

    public TableRefId Target { get; init; } = new("t1");

    public TableSourceRef Source { get; init; } = new();

    public bool? HasInsert { get; init; }

    public bool? HasUpdate { get; init; }

    public bool? HasDelete { get; init; }
}

public sealed record UnknownStatement : SqlStatement
{
    public UnknownStatement()
        : base(SqlStatementType.Unknown)
    {
    }
}

public sealed record TableRefId(string Value);

public enum TableRoleHint
{
    InsertTarget = 0,
    UpdateTarget = 1,
    DeleteTarget = 2,
    MergeTarget = 3,
    MergeSource = 4
}

public sealed record TableRef
{
    public TableRefId Id { get; init; } = new("t1");

    public TableSourceRef Source { get; init; } = new();

    public string? Alias { get; init; }

    public string? LogicalName { get; init; }

    public IReadOnlyList<TableRoleHint>? RoleHints { get; init; }

    public SourceSpan? Span { get; init; }
}

public enum TableSourceKind
{
    PhysicalTable = 0,
    DerivedTable = 1,
    Function = 2,
    Unknown = 999
}

public sealed record TableSourceRef
{
    public TableSourceKind Kind { get; init; } = TableSourceKind.Unknown;

    public QualifiedName? Name { get; init; }

    public string? ExpressionText { get; init; }

    public SourceSpan? Span { get; init; }
}

public sealed record QualifiedName
{
    public string? Database { get; init; }

    public string? Schema { get; init; }

    public string Object { get; init; } = string.Empty;

    public string Raw { get; init; } = string.Empty;
}

public sealed record TableRelation
{
    public TableRefId From { get; init; } = new("t1");

    public TableRefId To { get; init; } = new("t2");

    public JoinType JoinType { get; init; } = JoinType.Inner;

    public string? ConditionText { get; init; }

    public SourceSpan? Span { get; init; }
}

public enum JoinType
{
    Inner = 0,
    LeftOuter = 1,
    RightOuter = 2,
    FullOuter = 3,
    Cross = 4,
    CrossApply = 5,
    OuterApply = 6
}

public sealed record SelectItem
{
    public string? OutputName { get; init; }

    public string ExpressionText { get; init; } = string.Empty;

    public ColumnRef? SourceColumn { get; init; }

    public string? LogicalName { get; init; }

    public SourceSpan? Span { get; init; }
}

public sealed record ColumnRef
{
    public string ColumnName { get; init; } = string.Empty;

    public string? TableAliasOrName { get; init; }

    public TableRefId? ResolvedTable { get; init; }

    public SourceSpan? Span { get; init; }
}

public sealed record Diagnostic
{
    public DiagnosticSeverity Severity { get; init; } = DiagnosticSeverity.Info;

    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public SourceSpan? Span { get; init; }
}

public enum DiagnosticSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public sealed record SourceSpan
{
    public int StartIndex { get; init; }

    public int Length { get; init; }

    public int? StartLine { get; init; }

    public int? StartColumn { get; init; }
}
