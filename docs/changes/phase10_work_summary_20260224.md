# Phase 10 作業サマリ（20260224）

## 1) 実施概要

- 本フェーズは trimming（IL Linker）検証のみを実施。
- 機能追加・Domain変更・NuGet追加は行っていない。
- Baseline（非trimming）と Trimmed（PublishTrimmed=true）を比較し、サイズ差と実行可否を確認した。

## 2) Baseline publish（非trimming）

### 実行コマンド
```powershell
dotnet publish .\SqlAnalyzer.App\SqlAnalyzer.App.csproj -c Release -r win-x64 --self-contained true -o d:\github\SqlAnalyzerClient\artifacts\publish-baseline
```

### 結果
- publish: 成功
- 出力サイズ: `174,841,579 bytes`（`166.74 MB`）
- ファイル数: `482`
- 起動確認: 成功（`BASELINE_RUNNING_OK`）
- 基本フロー確認:
  - 起動時検証ハーネス（Phase4〜Phase8）が通過しているため、解析/抽出/図生成/フォーマットの既存基本動作は成立と判断

## 3) Trimmed publish（trimming有効）

### 実行コマンド
```powershell
dotnet publish .\SqlAnalyzer.App\SqlAnalyzer.App.csproj -c Release -r win-x64 --self-contained true -p:PublishTrimmed=true -p:TrimMode=partial -p:_SuppressWpfTrimError=true -o d:\github\SqlAnalyzerClient\artifacts\publish-trimmed
```

### 補足
- WPF は SDK 既定で trimming がエラー（NETSDK1168）となるため、検証目的で `_SuppressWpfTrimError=true` をコマンド指定して publish を実施。

### 結果
- publish: 成功
- 出力サイズ: `124,975,752 bytes`（`119.19 MB`）
- ファイル数: `399`
- 起動確認: **失敗**（`TRIMMED_EXITED:-532462766`）

## 4) サイズ比較（数値）

- Baseline: `174,841,579 bytes`（`166.74 MB`）
- Trimmed: `124,975,752 bytes`（`119.19 MB`）
- 差分: `49,865,827 bytes`（`47.56 MB`）削減
- 削減率: 約 `28.5%`

## 5) 出力構造の差分（概要）

- ファイル数: `482 -> 399`（`83` ファイル減）
- `Microsoft.SqlServer.TransactSql.ScriptDom.dll` は baseline/trimmed とも同梱
- trimmed でも ScriptDom リソース DLL 群は存在

## 6) 発生した問題（詳細）

### 症状
- Trimmed publish 版 EXE 起動時に即時例外終了

### 再現手順
1. Trimmed publish 実行
2. `artifacts\publish-trimmed\SqlAnalyzer.App.exe` を起動
3. 起動時に例外終了

### 取得ログ（要点）
- `System.Windows.Markup.XamlParseException`
- 内部例外: `InvalidOperationException: Phase6 verification failed: Case1 PNG render`
- 発生箇所: `SqlAnalyzer.App.MainWindow` コンストラクタで呼び出している `Phase6VerificationHarness.VerifyOrThrow()`

### 推定原因
- trimming により、起動時検証ハーネス内の PNG 描画検証条件が満たせず失敗している。
- 少なくとも現状の構成では、WPF + trimming の組み合わせで安全に起動できない。

## 7) 対応/回避策

- 安全回避策（現時点推奨）:
  - **本番配布では `PublishTrimmed` を有効化しない**（Phase9の非trimming publishを採用）
- 追跡候補:
  - 起動時ハーネスを配布ビルドから切り離し、検証専用実行に分離して trimming 影響を再評価
  - ただし本フェーズでは機能変更を行わない方針のため未対応

## 8) 変更ファイル一覧

### 追加
- `docs/changes/phase10_work_summary_20260224.md`

### 修正
- なし

### 削除
- なし

## 9) 実行コマンド一覧

```powershell
# Baseline
dotnet publish .\SqlAnalyzer.App\SqlAnalyzer.App.csproj -c Release -r win-x64 --self-contained true -o d:\github\SqlAnalyzerClient\artifacts\publish-baseline

# Trimmed
dotnet publish .\SqlAnalyzer.App\SqlAnalyzer.App.csproj -c Release -r win-x64 --self-contained true -p:PublishTrimmed=true -p:TrimMode=partial -p:_SuppressWpfTrimError=true -o d:\github\SqlAnalyzerClient\artifacts\publish-trimmed

# 起動確認
artifacts\publish-baseline\SqlAnalyzer.App.exe
artifacts\publish-trimmed\SqlAnalyzer.App.exe
```

## 10) 残タスク

- trimming を配布戦略に採用する場合は、WPF 起動時の検証処理を含めた trimming 互換性の再設計が必要。
- 次フェーズで方針決定（非trimming継続 or trimming互換化対応）を行う。
