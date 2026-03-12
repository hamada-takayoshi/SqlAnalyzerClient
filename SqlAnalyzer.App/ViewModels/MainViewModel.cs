using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SqlAnalyzer.Domain.Model;
using SqlAnalyzer.App.Services;
using SqlAnalyzer.SqlServer.Analysis;
using SqlAnalyzer.SqlServer.Boundary;
using SqlAnalyzer.SqlServer.Formatting;

namespace SqlAnalyzer.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ISqlAnalyzer _analyzer;
    private readonly StatementBoundaryExtractor _boundaryExtractor;
    private readonly DiagramService _diagramService;
    private readonly ExportService _exportService;
    private readonly ISqlFormatter _formatter;
    private readonly AsyncRelayCommand _analyzeCommand;
    private readonly RelayCommand _cancelCommand;
    private readonly RelayCommand _formatCommand;
    private readonly RelayCommand _settingsCommand;
    private readonly RelayCommand _copyMermaidCommand;
    private readonly RelayCommand _saveMermaidCommand;
    private readonly RelayCommand _savePngCommand;

    private string _sqlText = """
SELECT
    o.OrderId,
    c.CustomerName
FROM dbo.Orders o
LEFT JOIN dbo.Customers c ON o.CustomerId = c.CustomerId;
""";

    private SqlDialect _selectedDialect = SqlDialect.SqlServer;
    private string _statusText = "準備完了";
    private string _executionTimeText = "-";
    private string _boundaryInfoText = "-";
    private bool _isAnalyzing;
    private string _selectedDiagramMode = "図イメージ";
    private string _diagramErrorMessage = string.Empty;
    private string _mermaidText = "flowchart LR";
    private BitmapSource? _diagramImageSource;
    private byte[]? _diagramPngBytes;
    private SqlAnalysisResult? _analysisResult;
    private CancellationTokenSource? _analysisCancellationTokenSource;
    private int _formatIndentSize = 4;
    private bool _formatUppercaseKeywords = true;

    public MainViewModel(ISqlAnalyzer analyzer, ISqlFormatter formatter)
    {
        _analyzer = analyzer;
        _formatter = formatter;
        _boundaryExtractor = new StatementBoundaryExtractor();
        _diagramService = new DiagramService();
        _exportService = new ExportService();

        Dialects = new[] { SqlDialect.SqlServer };
        DiagramModes = new[] { "図イメージ", "Mermaidテキスト" };

        Tables = new ObservableCollection<TableRow>();
        Relations = new ObservableCollection<RelationRow>();
        SelectItems = new ObservableCollection<SelectItemRow>();
        Diagnostics = new ObservableCollection<DiagnosticRow>();

        _analyzeCommand = new AsyncRelayCommand(AnalyzeAsync, () => !IsAnalyzing);
        _cancelCommand = new RelayCommand(CancelAnalysis, () => IsAnalyzing);
        _formatCommand = new RelayCommand(FormatSql, () => !IsAnalyzing);
        _settingsCommand = new RelayCommand(() => OpenSettingsAction?.Invoke());
        _copyMermaidCommand = new RelayCommand(CopyMermaid, () => !string.IsNullOrWhiteSpace(MermaidText));
        _saveMermaidCommand = new RelayCommand(SaveMermaid, () => !string.IsNullOrWhiteSpace(MermaidText));
        _savePngCommand = new RelayCommand(SavePng, () => _diagramPngBytes is { Length: > 0 });
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

    public ICommand CopyMermaidCommand => _copyMermaidCommand;

    public ICommand SaveMermaidCommand => _saveMermaidCommand;

    public ICommand SavePngCommand => _savePngCommand;

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

    public bool IsDiagramImageMode => string.Equals(SelectedDiagramMode, "図イメージ", StringComparison.Ordinal);

    public bool IsMermaidMode => string.Equals(SelectedDiagramMode, "Mermaidテキスト", StringComparison.Ordinal);

    public string DiagramErrorMessage
    {
        get => _diagramErrorMessage;
        set => SetProperty(ref _diagramErrorMessage, value);
    }

    public string MermaidText
    {
        get => _mermaidText;
        set
        {
            if (SetProperty(ref _mermaidText, value))
            {
                _copyMermaidCommand.NotifyCanExecuteChanged();
                _saveMermaidCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public BitmapSource? DiagramImageSource
    {
        get => _diagramImageSource;
        set => SetProperty(ref _diagramImageSource, value);
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

    public string StatementTypeText => AnalysisResult is null
        ? "-"
        : ToStatementTypeText(AnalysisResult.Statement.StatementType);

    public int TableCount => AnalysisResult?.Statement.Tables.Count ?? 0;

    public int RelationCount => AnalysisResult?.Statement.Relations.Count ?? 0;

    public int SelectItemCount => (AnalysisResult?.Statement as SelectStatement)?.SelectItems.Count ?? 0;

    public int DiagnosticCount => AnalysisResult?.Diagnostics.Count ?? 0;

    public string BoundaryKindText => AnalysisResult is null
        ? "-"
        : ToBoundaryKindText(AnalysisResult.Document.Boundary.Kind);

    public bool IsSelectStatement => AnalysisResult?.Statement.StatementType == SqlStatementType.Select;

    public bool IsNotSelectStatement => !IsSelectStatement;

    public int FormatIndentSize
    {
        get => _formatIndentSize;
        set => SetProperty(ref _formatIndentSize, value);
    }

    public bool FormatUppercaseKeywords
    {
        get => _formatUppercaseKeywords;
        set => SetProperty(ref _formatUppercaseKeywords, value);
    }

    private void FormatSql()
    {
        StatusText = "整形中...";
        try
        {
            SqlFormatResult formatResult = _formatter.FormatAsync(
                    SelectedDialect,
                    SqlText,
                    new SqlFormatOptions
                    {
                        IndentationWidth = FormatIndentSize,
                        UppercaseKeywords = FormatUppercaseKeywords
                    },
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            SqlText = formatResult.FormattedSql;
            foreach (Diagnostic diagnostic in formatResult.Diagnostics)
            {
                AddOrUpdateDiagnostic(diagnostic);
            }

            StatusText = formatResult.Diagnostics.Count == 0
                ? "整形が完了しました。"
                : "整形が完了しました（診断あり）。";
        }
        catch (Exception ex)
        {
            AddOrUpdateDiagnostic(new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = "UNSUPPORTED_SYNTAX",
                Message = $"整形中に予期しないエラーが発生しました: {ex.Message}"
            });
            StatusText = "整形に失敗しました。";
        }
    }

    private async Task AnalyzeAsync()
    {
        _analysisCancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = _analysisCancellationTokenSource.Token;
        StatementBoundaryExtractionResult boundaryResult = _boundaryExtractor.Extract(SqlText);

        Stopwatch stopwatch = Stopwatch.StartNew();
        IsAnalyzing = true;
        StatusText = "解析中...";
        ExecutionTimeText = "-";

        try
        {
            SqlAnalysisResult result = await _analyzer.AnalyzeAsync(SelectedDialect, boundaryResult.NormalizedText, cancellationToken);
            result = MergeBoundaryAndDiagnostics(result, boundaryResult);
            stopwatch.Stop();

            ApplyResult(result);
            StatusText = "準備完了";
            ExecutionTimeText = $"{stopwatch.ElapsedMilliseconds} ms";
            BoundaryInfoText = $"解析範囲: {ToBoundaryKindText(result.Document.Boundary.Kind)}まで";
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            StatusText = "キャンセルしました";
            ExecutionTimeText = $"{stopwatch.ElapsedMilliseconds} ms";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            SqlAnalysisResult fallback = CreateFallbackResult(boundaryResult, ex);
            ApplyResult(fallback);
            StatusText = "準備完了（診断あり）";
            ExecutionTimeText = $"{stopwatch.ElapsedMilliseconds} ms";
            BoundaryInfoText = $"解析範囲: {ToBoundaryKindText(fallback.Document.Boundary.Kind)}まで";
        }
        finally
        {
            IsAnalyzing = false;
            _analysisCancellationTokenSource?.Dispose();
            _analysisCancellationTokenSource = null;
        }
    }

    private static SqlAnalysisResult MergeBoundaryAndDiagnostics(
        SqlAnalysisResult result,
        StatementBoundaryExtractionResult boundaryResult)
    {
        List<Diagnostic> diagnostics = result.Diagnostics.ToList();

        if (boundaryResult.HasTrailingStatements &&
            diagnostics.All(d => !string.Equals(d.Code, "MULTI_STATEMENT_TRUNCATED", StringComparison.Ordinal)))
        {
            diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Code = "MULTI_STATEMENT_TRUNCATED",
                Message = "最初のSQLステートメントのみ解析しました。後続のステートメントは無視されました。"
            });
        }

        return result with
        {
            Document = new SqlDocumentInfo
            {
                Boundary = boundaryResult.Boundary,
                HasTrailingStatements = boundaryResult.HasTrailingStatements
            },
            Diagnostics = diagnostics
        };
    }

    private SqlAnalysisResult CreateFallbackResult(StatementBoundaryExtractionResult boundaryResult, Exception ex)
    {
        SqlAnalysisResult fallback = new()
        {
            Dialect = SelectedDialect,
            Document = new SqlDocumentInfo
            {
                Boundary = boundaryResult.Boundary,
                HasTrailingStatements = boundaryResult.HasTrailingStatements
            },
            Statement = new UnknownStatement(),
            Diagnostics = new[]
            {
                new Diagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Code = "UNSUPPORTED_SYNTAX",
                    Message = $"解析中に予期しないエラーが発生しました: {ex.Message}"
                }
            }
        };

        return MergeBoundaryAndDiagnostics(fallback, boundaryResult);
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
                Role = table.RoleHints is { Count: > 0 }
                    ? string.Join(", ", table.RoleHints.Select(ToTableRoleHintText))
                    : "-"
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
                JoinType = ToJoinTypeText(relation.JoinType),
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
                    ResolutionStatus = item.SourceColumn?.ResolvedTable is not null ? "解決済み" : "未解決"
                });
            }
        }

        foreach (Diagnostic diagnostic in result.Diagnostics)
        {
            string location = diagnostic.Span is null
                ? "-"
                : $"位置 {diagnostic.Span.StartIndex}, 長さ {diagnostic.Span.Length}";

            Diagnostics.Add(new DiagnosticRow
            {
                Severity = ToDiagnosticSeverityText(diagnostic.Severity),
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Location = location
            });
        }

        DiagramArtifacts diagram = _diagramService.Generate(result.Statement);
        MermaidText = diagram.MermaidText;
        _diagramPngBytes = diagram.PngBytes;
        DiagramImageSource = _diagramService.CreateBitmapSource(diagram.PngBytes);
        DiagramErrorMessage = diagram.ErrorMessage ?? string.Empty;
        DebugLog.Write(
            $"ApplyResult diagram: statement={result.Statement.StatementType}, tables={result.Statement.Tables.Count}, relations={result.Statement.Relations.Count}, " +
            $"pngBytes={(_diagramPngBytes?.Length ?? 0)}, imageNull={(DiagramImageSource is null)}, error='{DiagramErrorMessage}'");
        _savePngCommand.NotifyCanExecuteChanged();
    }

    private void AddOrUpdateDiagnostic(Diagnostic diagnostic)
    {
        Diagnostics.Add(new DiagnosticRow
        {
            Severity = ToDiagnosticSeverityText(diagnostic.Severity),
            Code = diagnostic.Code,
            Message = diagnostic.Message,
            Location = diagnostic.Span is null ? "-" : $"位置 {diagnostic.Span.StartIndex}, 長さ {diagnostic.Span.Length}"
        });
    }

    private void CopyMermaid()
    {
        if (string.IsNullOrWhiteSpace(MermaidText))
        {
            StatusText = "コピー対象のMermaidテキストがありません。";
            return;
        }

        const int maxAttempts = 5;
        const int retryDelayMs = 120;

        COMException? lastComException = null;
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Clipboard.SetText(MermaidText);
                StatusText = attempt == 1
                    ? "Mermaidテキストをコピーしました。"
                    : $"Mermaidテキストをコピーしました（{attempt - 1}回リトライ後に成功）。";
                return;
            }
            catch (COMException ex)
            {
                lastComException = ex;
                if (attempt < maxAttempts)
                {
                    Thread.Sleep(retryDelayMs);
                    continue;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < maxAttempts)
                {
                    Thread.Sleep(retryDelayMs);
                    continue;
                }
            }
        }

        if (lastComException is not null)
        {
            StatusText = $"クリップボードにコピーできませんでした（{maxAttempts}回試行）。HResult: 0x{lastComException.HResult:X8}";
            return;
        }

        StatusText = lastException is null
            ? $"クリップボードにコピーできませんでした（{maxAttempts}回試行）。"
            : $"クリップボードにコピーできませんでした（{maxAttempts}回試行）。{lastException.Message}";
    }

    private void SaveMermaid()
    {
        if (_exportService.SaveMermaidMarkdown(MermaidText))
        {
            StatusText = "Mermaid Markdownを保存しました。";
        }
    }

    private void SavePng()
    {
        if (_diagramPngBytes is { Length: > 0 } && _exportService.SavePng(_diagramPngBytes))
        {
            StatusText = "PNGを保存しました。";
            return;
        }

        StatusText = "PNG保存に失敗したか、キャンセルされました。";
    }

    private static string ToStatementTypeText(SqlStatementType statementType) =>
        statementType switch
        {
            SqlStatementType.Select => "SELECT",
            SqlStatementType.Insert => "INSERT",
            SqlStatementType.Update => "UPDATE",
            SqlStatementType.Delete => "DELETE",
            SqlStatementType.Merge => "MERGE",
            SqlStatementType.Unknown => "不明",
            _ => "不明"
        };

    private static string ToBoundaryKindText(BoundaryKind boundaryKind) =>
        boundaryKind switch
        {
            BoundaryKind.Semicolon => "セミコロン",
            BoundaryKind.GoBatch => "GOバッチ",
            BoundaryKind.EndOfText => "テキスト終端",
            BoundaryKind.Unknown => "不明",
            _ => "不明"
        };

    private static string ToJoinTypeText(JoinType joinType) =>
        joinType switch
        {
            JoinType.Inner => "内部結合",
            JoinType.LeftOuter => "左外部結合",
            JoinType.RightOuter => "右外部結合",
            JoinType.FullOuter => "完全外部結合",
            JoinType.Cross => "クロス結合",
            JoinType.CrossApply => "CROSS APPLY",
            JoinType.OuterApply => "OUTER APPLY",
            _ => "不明"
        };

    private static string ToTableRoleHintText(TableRoleHint roleHint) =>
        roleHint switch
        {
            TableRoleHint.InsertTarget => "INSERT対象",
            TableRoleHint.UpdateTarget => "UPDATE対象",
            TableRoleHint.DeleteTarget => "DELETE対象",
            TableRoleHint.MergeTarget => "MERGE対象",
            TableRoleHint.MergeSource => "MERGEソース",
            _ => "不明"
        };

    private static string ToDiagnosticSeverityText(DiagnosticSeverity severity) =>
        severity switch
        {
            DiagnosticSeverity.Info => "情報",
            DiagnosticSeverity.Warning => "警告",
            DiagnosticSeverity.Error => "エラー",
            _ => "不明"
        };

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

        public string ResolutionStatus { get; init; } = "未解決";
    }

    public sealed record DiagnosticRow
    {
        public string Severity { get; init; } = "情報";

        public string Code { get; init; } = "-";

        public string Message { get; init; } = "-";

        public string Location { get; init; } = "-";
    }
}
