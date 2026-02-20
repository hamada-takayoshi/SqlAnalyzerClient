using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using SqlAnalyzer.Domain.Model;
using SqlAnalyzer.SqlServer.Analysis;

namespace SqlAnalyzer.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ISqlAnalyzer _analyzer;
    private readonly AsyncRelayCommand _analyzeCommand;
    private readonly RelayCommand _cancelCommand;
    private readonly RelayCommand _formatCommand;
    private readonly RelayCommand _settingsCommand;

    private string _sqlText = """
SELECT
    o.OrderId,
    c.CustomerName
FROM dbo.Orders o
LEFT JOIN dbo.Customers c ON o.CustomerId = c.CustomerId;
""";

    private SqlDialect _selectedDialect = SqlDialect.SqlServer;
    private string _statusText = "Ready";
    private string _executionTimeText = "-";
    private string _boundaryInfoText = "-";
    private bool _isAnalyzing;
    private string _selectedDiagramMode = "Diagram Image";
    private string _diagramImageText = "Diagram generation is not implemented in Phase 2.";
    private string _mermaidText = "%% Mermaid generation is not implemented in Phase 2.";
    private SqlAnalysisResult? _analysisResult;
    private CancellationTokenSource? _analysisCancellationTokenSource;

    public MainViewModel(ISqlAnalyzer analyzer)
    {
        _analyzer = analyzer;

        Dialects = new[] { SqlDialect.SqlServer };
        DiagramModes = new[] { "Diagram Image", "Mermaid Markdown" };

        Tables = new ObservableCollection<TableRow>();
        Relations = new ObservableCollection<RelationRow>();
        SelectItems = new ObservableCollection<SelectItemRow>();
        Diagnostics = new ObservableCollection<DiagnosticRow>();

        _analyzeCommand = new AsyncRelayCommand(AnalyzeAsync, () => !IsAnalyzing);
        _cancelCommand = new RelayCommand(CancelAnalysis, () => IsAnalyzing);
        _formatCommand = new RelayCommand(FormatSql, () => !IsAnalyzing);
        _settingsCommand = new RelayCommand(() => OpenSettingsAction?.Invoke());
    }

    public Action? OpenSettingsAction { get; set; }

    public IEnumerable<SqlDialect> Dialects { get; }

    public IEnumerable<string> DiagramModes { get; }

    public ObservableCollection<TableRow> Tables { get; }

    public ObservableCollection<RelationRow> Relations { get; }

    public ObservableCollection<SelectItemRow> SelectItems { get; }

    public ObservableCollection<DiagnosticRow> Diagnostics { get; }

    public ICommand FormatCommand => _formatCommand;

    public ICommand AnalyzeCommand => _analyzeCommand;

    public ICommand CancelCommand => _cancelCommand;

    public ICommand SettingsCommand => _settingsCommand;

    public string SqlText
    {
        get => _sqlText;
        set => SetProperty(ref _sqlText, value);
    }

    public SqlDialect SelectedDialect
    {
        get => _selectedDialect;
        set => SetProperty(ref _selectedDialect, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string ExecutionTimeText
    {
        get => _executionTimeText;
        set => SetProperty(ref _executionTimeText, value);
    }

    public string BoundaryInfoText
    {
        get => _boundaryInfoText;
        set => SetProperty(ref _boundaryInfoText, value);
    }

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        private set
        {
            if (SetProperty(ref _isAnalyzing, value))
            {
                _analyzeCommand.NotifyCanExecuteChanged();
                _cancelCommand.NotifyCanExecuteChanged();
                _formatCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string SelectedDiagramMode
    {
        get => _selectedDiagramMode;
        set
        {
            if (SetProperty(ref _selectedDiagramMode, value))
            {
                OnPropertyChanged(nameof(IsDiagramImageMode));
                OnPropertyChanged(nameof(IsMermaidMode));
            }
        }
    }

    public bool IsDiagramImageMode => string.Equals(SelectedDiagramMode, "Diagram Image", StringComparison.Ordinal);

    public bool IsMermaidMode => string.Equals(SelectedDiagramMode, "Mermaid Markdown", StringComparison.Ordinal);

    public string DiagramImageText
    {
        get => _diagramImageText;
        set => SetProperty(ref _diagramImageText, value);
    }

    public string MermaidText
    {
        get => _mermaidText;
        set => SetProperty(ref _mermaidText, value);
    }

    public SqlAnalysisResult? AnalysisResult
    {
        get => _analysisResult;
        private set
        {
            if (SetProperty(ref _analysisResult, value))
            {
                OnPropertyChanged(nameof(StatementTypeText));
                OnPropertyChanged(nameof(TableCount));
                OnPropertyChanged(nameof(RelationCount));
                OnPropertyChanged(nameof(SelectItemCount));
                OnPropertyChanged(nameof(DiagnosticCount));
                OnPropertyChanged(nameof(BoundaryKindText));
                OnPropertyChanged(nameof(IsSelectStatement));
                OnPropertyChanged(nameof(IsNotSelectStatement));
            }
        }
    }

    public string StatementTypeText => AnalysisResult?.Statement.StatementType.ToString() ?? "-";

    public int TableCount => AnalysisResult?.Statement.Tables.Count ?? 0;

    public int RelationCount => AnalysisResult?.Statement.Relations.Count ?? 0;

    public int SelectItemCount => (AnalysisResult?.Statement as SelectStatement)?.SelectItems.Count ?? 0;

    public int DiagnosticCount => AnalysisResult?.Diagnostics.Count ?? 0;

    public string BoundaryKindText => AnalysisResult?.Document.Boundary.Kind.ToString() ?? "-";

    public bool IsSelectStatement => AnalysisResult?.Statement.StatementType == SqlStatementType.Select;

    public bool IsNotSelectStatement => !IsSelectStatement;

    private void FormatSql()
    {
        StatusText = "Formatting is not implemented in Phase 2.";
    }

    private async Task AnalyzeAsync()
    {
        _analysisCancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = _analysisCancellationTokenSource.Token;

        Stopwatch stopwatch = Stopwatch.StartNew();
        IsAnalyzing = true;
        StatusText = "Analyzing...";
        ExecutionTimeText = "-";

        try
        {
            SqlAnalysisResult result = await _analyzer.AnalyzeAsync(SelectedDialect, SqlText, cancellationToken);
            stopwatch.Stop();

            ApplyResult(result);
            StatusText = "Ready";
            ExecutionTimeText = $"{stopwatch.ElapsedMilliseconds} ms";
            BoundaryInfoText = $"Analyzed until {result.Document.Boundary.Kind}";
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            StatusText = "Canceled";
            ExecutionTimeText = $"{stopwatch.ElapsedMilliseconds} ms";
        }
        finally
        {
            IsAnalyzing = false;
            _analysisCancellationTokenSource?.Dispose();
            _analysisCancellationTokenSource = null;
        }
    }

    private void CancelAnalysis()
    {
        _analysisCancellationTokenSource?.Cancel();
    }

    private void ApplyResult(SqlAnalysisResult result)
    {
        AnalysisResult = result;

        Tables.Clear();
        Relations.Clear();
        SelectItems.Clear();
        Diagnostics.Clear();

        Dictionary<string, TableRef> tableMap = result.Statement.Tables.ToDictionary(t => t.Id.Value, t => t);

        foreach (TableRef table in result.Statement.Tables)
        {
            string displayName = table.Alias
                ?? table.Source.Name?.Object
                ?? table.Source.ExpressionText
                ?? table.Id.Value;

            Tables.Add(new TableRow
            {
                DisplayName = displayName,
                Schema = table.Source.Name?.Schema ?? "-",
                Object = table.Source.Name?.Object ?? table.Source.ExpressionText ?? "-",
                Alias = table.Alias ?? "-",
                LogicalName = table.LogicalName ?? "-",
                Role = table.RoleHints is { Count: > 0 } ? string.Join(", ", table.RoleHints) : "-"
            });
        }

        foreach (TableRelation relation in result.Statement.Relations)
        {
            string fromName = tableMap.TryGetValue(relation.From.Value, out TableRef? fromTable)
                ? (fromTable.Alias ?? fromTable.Source.Name?.Object ?? relation.From.Value)
                : relation.From.Value;
            string toName = tableMap.TryGetValue(relation.To.Value, out TableRef? toTable)
                ? (toTable.Alias ?? toTable.Source.Name?.Object ?? relation.To.Value)
                : relation.To.Value;

            Relations.Add(new RelationRow
            {
                FromTable = fromName,
                JoinType = relation.JoinType.ToString(),
                ToTable = toName
            });
        }

        if (result.Statement is SelectStatement selectStatement)
        {
            foreach (SelectItem item in selectStatement.SelectItems)
            {
                SelectItems.Add(new SelectItemRow
                {
                    OutputName = item.OutputName ?? "-",
                    Expression = item.ExpressionText,
                    Table = item.SourceColumn?.TableAliasOrName ?? "-",
                    ColumnName = item.SourceColumn?.ColumnName ?? "-",
                    LogicalName = item.LogicalName ?? "-",
                    ResolutionStatus = item.SourceColumn?.ResolvedTable is not null ? "Resolved" : "Unresolved"
                });
            }
        }

        foreach (Diagnostic diagnostic in result.Diagnostics)
        {
            string location = diagnostic.Span is null
                ? "-"
                : $"Index {diagnostic.Span.StartIndex}, Len {diagnostic.Span.Length}";

            Diagnostics.Add(new DiagnosticRow
            {
                Severity = diagnostic.Severity.ToString(),
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Location = location
            });
        }

        MermaidText = "%% Dummy output for Phase 2\ngraph TD\n  t1[Orders] -->|LeftOuter| t2[Customers]";
        DiagramImageText = "Diagram image generation is not implemented in Phase 2.";
    }

    public sealed record TableRow
    {
        public string DisplayName { get; init; } = "-";

        public string Schema { get; init; } = "-";

        public string Object { get; init; } = "-";

        public string Alias { get; init; } = "-";

        public string LogicalName { get; init; } = "-";

        public string Role { get; init; } = "-";
    }

    public sealed record RelationRow
    {
        public string FromTable { get; init; } = "-";

        public string JoinType { get; init; } = "-";

        public string ToTable { get; init; } = "-";
    }

    public sealed record SelectItemRow
    {
        public string OutputName { get; init; } = "-";

        public string Expression { get; init; } = "-";

        public string Table { get; init; } = "-";

        public string ColumnName { get; init; } = "-";

        public string LogicalName { get; init; } = "-";

        public string ResolutionStatus { get; init; } = "Unresolved";
    }

    public sealed record DiagnosticRow
    {
        public string Severity { get; init; } = "Info";

        public string Code { get; init; } = "-";

        public string Message { get; init; } = "-";

        public string Location { get; init; } = "-";
    }
}
