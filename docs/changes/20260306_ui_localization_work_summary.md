# UI日本語化 作業サマリ（2026-03-06）

## 概要
- `SqlAnalyzer.App` のUI文言（ボタン名、タブ名、ラベル、ステータス、保存ダイアログ文言）を英語から日本語へ統一。
- 仕様確認として以下を順番に読込:
  - `AGENTS.md`
  - `docs/00_overview.md`
  - `docs/01_requirements.md`
  - `docs/03_architecture.md`
  - `docs/04_domain_model.md`
  - `docs/05_ui_design.md`
  - `docs/13_task_plan.md`

## 変更ファイル
- `SqlAnalyzer.App/MainWindow.xaml`
- `SqlAnalyzer.App/Views/SettingsWindow.xaml`
- `SqlAnalyzer.App/ViewModels/MainViewModel.cs`
- `SqlAnalyzer.App/Services/ExportService.cs`

## 主な変更内容
- メイン画面
  - `Format/Analyze/Cancel/Settings` -> `整形/解析/キャンセル/設定`
  - タブ名 `Summary/Tables/Relations/Select Items/Diagram/Diagnostics` を日本語化
  - DataGridヘッダー、説明文、出力関連ボタン文言を日本語化
- 設定画面
  - タブ名・ラベル・チェックボックス文言を日本語化
- ViewModel表示文言
  - `Ready`, `Formatting...`, `Analyzing...`, `Canceled` 等のステータスを日本語化
  - 境界種別、JOIN種別、RoleHint、診断重要度、解決状態の表示用文言を日本語化
  - 固定診断メッセージ（多重文、例外時）を日本語化
- 保存ダイアログ
  - Mermaid/PNG保存のタイトルとフィルター文言を日本語化

## 検証結果
- `dotnet build` : 成功（0 warnings / 0 errors）
- `dotnet test` : 成功（7 passed / 0 failed / 0 skipped）

## 補足
- アーキテクチャ境界（App/Domain/SqlServer）やオフライン制約に影響する変更はなし。
