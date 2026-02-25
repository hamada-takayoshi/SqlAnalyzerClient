# Phase 9 作業サマリ（20260224）

## 1) 実施内容の概要

- 本フェーズでは機能追加は行わず、Self-contained 配布検証のみ実施。
- `SqlAnalyzer.App`（WPF, `net8.0-windows`）を `win-x64` 向けに Self-contained publish した。
- publish 出力物の構成、サイズ、直接実行可否を確認した。

## 2) Publish コマンド

実行コマンド:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

## 3) Build/Publish 結果

- publish: 成功
- エラー: なし
- 出力先:
  - `SqlAnalyzer.App/bin/Release/net8.0-windows/win-x64/publish`

## 4) 出力構成の確認

### アプリ実行ファイル
- `SqlAnalyzer.App.exe` が生成されていることを確認

### 主要同梱ファイル
- `SqlAnalyzer.App.dll`
- `SqlAnalyzer.Domain.dll`
- `SqlAnalyzer.SqlServer.dll`
- `Microsoft.SqlServer.TransactSql.ScriptDom.dll`
- `SqlAnalyzer.App.deps.json`
- `SqlAnalyzer.App.runtimeconfig.json`

### Self-contained 設定確認
- `SqlAnalyzer.App.runtimeconfig.json` に `includedFrameworks` が含まれていることを確認
  - `Microsoft.NETCore.App` 8.0.19
  - `Microsoft.WindowsDesktop.App` 8.0.19

## 5) クリーン環境前提チェック

- PATH 依存なし:
  - publish フォルダ内の `SqlAnalyzer.App.exe` を直接起動し確認
- 開発時のみ参照の依存欠落なし:
  - 必要 DLL 群が publish に同梱されていることを確認
- ScriptDom 同梱確認:
  - `Microsoft.SqlServer.TransactSql.ScriptDom.dll` と多言語リソース DLL が存在
- 設定ファイル確認:
  - `.deps.json` / `.runtimeconfig.json` が存在

## 6) 配布サイズ確認

- publish フォルダ総サイズ: **166.74 MB**
- 拡張子内訳（上位）:
  - `.dll`: 475
  - `.pdb`: 3
  - `.exe`: 2
  - `.json`: 2
- 主な大容量ファイル（抜粋）:
  - `PresentationFramework.dll`
  - `System.Windows.Forms.dll`
  - `System.Private.CoreLib.dll`
  - `Microsoft.SqlServer.TransactSql.ScriptDom.dll`

## 7) 最小実行テスト結果

実行方法:
- publish フォルダの `SqlAnalyzer.App.exe` を直接起動

結果:
- 起動確認成功（`PUBLISH_EXE_RUNNING_OK`）
- 実行時例外による即時終了なし

## 8) 問題点

- 重大な問題なし
- 補足:
  - Release publish に `.pdb` が 3 ファイル含まれる（配布方針に応じて除外検討余地あり）

## 9) 次フェーズ向けメモ

- Phase10 で構造整理・不要ファイル整理・コメント整備を実施する場合、配布サイズ最適化方針（PDB含有可否など）を明確化するとよい。
