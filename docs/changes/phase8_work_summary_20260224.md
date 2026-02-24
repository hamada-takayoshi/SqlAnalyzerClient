# Phase 8 作業サマリ（20260224）

## 1) 変更ファイル一覧

### 追加
- `SqlAnalyzer.SqlServer/Formatting/ISqlFormatter.cs`
- `SqlAnalyzer.SqlServer/Formatting/SqlFormatOptions.cs`
- `SqlAnalyzer.SqlServer/Formatting/SqlFormatResult.cs`
- `SqlAnalyzer.SqlServer/Formatting/SqlServerFormatter.cs`
- `SqlAnalyzer.App/Verification/Phase8VerificationHarness.cs`
- `docs/changes/phase8_work_summary_20260224.md`

### 修正
- `SqlAnalyzer.App/ViewModels/MainViewModel.cs`
- `SqlAnalyzer.App/MainWindow.xaml.cs`

### 削除
- なし

## 2) 変更内容の概要

- Phase8 要件に合わせて SQL Server フォーマッタを実装した。
- `SqlAnalyzer.SqlServer.Formatting` にフォーマット用インターフェースと結果モデルを追加した。
- `SqlServerFormatter` で ScriptDom パース + ScriptGenerator による整形を実装した。
- 設定項目として以下を反映した。
  - インデント幅
  - キーワード大文字化 ON/OFF
- フォーマット失敗時の診断を返却する実装にした。
  - 主に `PARTIAL_PARSE` / `UNSUPPORTED_SYNTAX`
- `MainViewModel` の `Format` ボタン処理を実装し、成功時に `SqlText` を更新するようにした。
- フォーマット診断を Diagnostics 表示リストへ反映する処理を追加した。
- Domain モデルの変更は行っていない。

## 3) 検証手順

### 起動時ハーネス（Phase8VerificationHarness）
- 大文字整形（UppercaseKeywords=true）
  - `SELECT` が大文字で出力されることを確認
- 小文字整形（UppercaseKeywords=false）
  - `select` が小文字で出力されることを確認
- 不正SQL整形
  - 診断が返ること
  - `PARTIAL_PARSE` または `UNSUPPORTED_SYNTAX` が含まれること

## 4) 実行コマンド

- `dotnet restore`
- `dotnet build`
- `dotnet run --project .\SqlAnalyzer.App`

## 5) build/run 結果

- build: 成功（警告 0 / エラー 0）
- run: 成功（`RUNNING_OK`）

## 6) 残タスク

- Phase9（配布/パッケージング最終確認）は未対応
- Settings 画面とフォーマット設定値の双方向連携は未対応（現在は ViewModel プロパティで保持）
- フォーマット失敗時の診断メッセージ詳細化（ユーザー向け表現の改善）は余地あり
