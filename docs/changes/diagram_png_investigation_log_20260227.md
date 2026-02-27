# Diagram PNG表示/保存 調査ログ追加サマリー (2026-02-27)

## 1. 変更ファイル

### 追加
- `SqlAnalyzer.App/Services/DebugLog.cs`

### 更新
- `SqlAnalyzer.App/Services/DiagramService.cs`
- `SqlAnalyzer.App/Services/ExportService.cs`
- `SqlAnalyzer.App/ViewModels/MainViewModel.cs`

## 2. 実施内容

ユーザー報告（テーブル2件はMermaid表示されるがDiagramImage表示とPNG保存が不安定）に対し、
ソース解析だけで断定しきれないため、PNG生成〜Bitmap化〜保存の各段階にデバッグログを追加した。

追加した観測点:
- Diagram生成開始時（statement種別、table数、relation数）
- Diagram生成成功時（PNGバイト長、Mermaid長）
- Diagram生成例外時（例外詳細）
- BitmapSource生成スキップ/成功/例外
- MainViewModel.ApplyResultでの反映結果（画像null判定、エラーメッセージ）
- PNG保存の成功/キャンセル/失敗（保存先、バイト長、例外）

ログ出力先:
- `AppContext.BaseDirectory\\diagram_debug.log`

## 3. 解析時点の確認結果

`dotnet test` 実行時に生成されたログ（`SqlAnalyzer.Tests/bin/Debug/net8.0-windows/diagram_debug.log`）では、
以下を確認:
- `tables=2` でも `Generate success` になり、PNGバイトが生成されるケースが存在

よって、少なくとも一部ケースでは「テーブルあり＝PNG生成失敗」ではない。
実アプリ実行時のログを取得し、
- Generate は成功しているが CreateBitmapSource で失敗しているか
- 生成/Bitmap化は成功しているが表示モードやUI反映で見えていないか
- 保存時のIO失敗か
を切り分ける。

## 4. 実行コマンド

1. `Get-Content AGENTS.md`
2. `Get-Content docs/00_overview.md`
3. `Get-Content docs/01_requirements.md`
4. `Get-Content docs/03_architecture.md`
5. `Get-Content docs/04_domain_model.md`
6. `Get-Content docs/05_ui_design.md`
7. `Get-Content docs/13_task_plan.md`
8. `dotnet build SqlAnalyzerClient.sln`
9. `dotnet test SqlAnalyzerClient.sln`

## 5. build/test結果

- `dotnet build` 成功（0 Warning / 0 Error）
- `dotnet test` 成功（7 passed / 0 failed）

## 6. 残件

- 実アプリ（Visual Studio起動）で再現時の `diagram_debug.log` を取得し、表示不具合の失敗点を確定する。
