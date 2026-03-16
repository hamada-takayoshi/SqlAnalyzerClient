# net462移行後の build/test 失敗調査メモ

## 日付
- 2026-03-17

## 背景
- `.NET Framework 4.6.2 (net462)` へ移行後、`dotnet build` / `dotnet test` が失敗する。

## 確認した事象
- `dotnet build` / `dotnet test` が「0 warning / 0 error」のまま終了コード `1` で失敗。
- 詳細ログでは Restore フェーズ（`_FilterRestoreGraphProjectInputItems`）で失敗。
- 以前は `NU1301`（NuGet TLS/証明書関連）も確認されている。
- `SqlAnalyzer.Domain` / `SqlAnalyzer.SqlServer` 単体ビルドは通るが、`SqlAnalyzer.App` / `SqlAnalyzer.Tests` は Restore で失敗。

## 実施した対策
- リポジトリ直下に `Directory.Build.props` を追加し、Restore の再現性を改善:
  - `RestorePackagesPath` をリポジトリ内 `.nuget/packages` に固定
  - `RestoreAdditionalProjectSources` にローカル NuGet キャッシュ (`%USERPROFILE%\.nuget\packages`) を追加
  - `RestoreIgnoreFailedSources=true` を設定
  - `NuGetAudit=false` を設定（オフライン時のノイズ低減）
- `global.json` を追加し、SDK を `8.0.418` に固定（`9.0.304` 由来の挙動差回避）。
- `.gitignore` に `.nuget/` を追加（復元先の成果物をコミットしない）。

## 変更ファイル
- `Directory.Build.props`（新規）
- `global.json`（新規）
- `.gitignore`（更新）

## 残課題
- この実行環境では `SqlAnalyzer.App` / `SqlAnalyzer.Tests` の Restore 失敗原因を特定し切れず、`dotnet build` / `dotnet test` の完全成功までは未到達。
- ユーザー端末で以下を確認して最終確定する:
  1. `dotnet --version` が `8.0.418` になること（`global.json` 有効化）
  2. `dotnet build`
  3. `dotnet test`

## 補足
- `net462` では `Microsoft.NETFramework.ReferenceAssemblies` 系依存の復元成否が重要。
- ローカルキャッシュを追加復元ソースに使う構成は、オフライン/制限環境で有効。
