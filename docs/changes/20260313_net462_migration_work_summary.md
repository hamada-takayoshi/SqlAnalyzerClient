# .NET Framework 4.6.2 対応作業サマリ（2026-03-13）

## 1. 目的

アプリケーション全体を `.NET Framework 4.6.2` でビルド・テスト実行できる状態へ移行した。

## 2. 実施内容

### 2.1 ドキュメント更新（docs優先ルール対応）

- `docs/01_requirements.md`
  - 配布要件を self-contained 前提から `.NET Framework 4.6.2` 前提へ変更
  - 例示コマンドを `dotnet publish` から `dotnet build -c Release` へ変更
- `docs/03_architecture.md`
  - 段階的実装戦略のパッケージ記述を `.NET Framework 4.6.2` のオフライン配布向けへ更新
- `docs/13_task_plan.md`
  - Phase 0 のターゲットを `.NET 8` から `.NET Framework 4.6.2` へ更新
  - Phase 9 を「Self-Contained Packaging」から「Offline Packaging」へ更新
  - Phase 9 の手順・DoDを `.NET Framework 4.6.2` 前提へ更新

### 2.2 プロジェクト設定変更

- `SqlAnalyzer.Domain/SqlAnalyzer.Domain.csproj` を `net462` へ変更
- `SqlAnalyzer.SqlServer/SqlAnalyzer.SqlServer.csproj` を `net462` へ変更
- `SqlAnalyzer.App/SqlAnalyzer.App.csproj`
  - SDK を `Microsoft.NET.Sdk.WindowsDesktop` へ変更
  - TargetFramework を `net462` へ変更
- `SqlAnalyzer.Tests/SqlAnalyzer.Tests.csproj` を `net462` へ変更

### 2.3 互換性修正（.NET Framework 4.6.2）

- record/init 利用継続のため `IsExternalInit` を追加
  - `SqlAnalyzer.Domain/Compatibility/IsExternalInit.cs`
  - `SqlAnalyzer.SqlServer/Compatibility/IsExternalInit.cs`
  - `SqlAnalyzer.App/Compatibility/IsExternalInit.cs`
- .NET Framework 非対応構文/APIの置換
  - `Span`/`ReadOnlySpan`/Range(Index) 依存の除去
  - `string.Contains(value, StringComparison)` の代替として `IndexOf` を使用
  - C#12 collection expression (`[]`) を通常初期化へ置換

## 3. 変更ファイル

- `docs/01_requirements.md`
- `docs/03_architecture.md`
- `docs/13_task_plan.md`
- `SqlAnalyzer.Domain/SqlAnalyzer.Domain.csproj`
- `SqlAnalyzer.SqlServer/SqlAnalyzer.SqlServer.csproj`
- `SqlAnalyzer.App/SqlAnalyzer.App.csproj`
- `SqlAnalyzer.Tests/SqlAnalyzer.Tests.csproj`
- `SqlAnalyzer.SqlServer/Boundary/StatementBoundaryExtractor.cs`
- `SqlAnalyzer.App/Services/DiagramService.cs`
- `SqlAnalyzer.SqlServer/Analysis/SqlStatementDomainMapper.cs`
- `SqlAnalyzer.SqlServer/Analysis/SqlServerAnalyzer.cs`
- `SqlAnalyzer.Tests/Tests/Infrastructure/Phase6VerificationHarness.cs`
- `SqlAnalyzer.Tests/Tests/Infrastructure/Phase8VerificationHarness.cs`
- `SqlAnalyzer.Domain/Compatibility/IsExternalInit.cs`
- `SqlAnalyzer.SqlServer/Compatibility/IsExternalInit.cs`
- `SqlAnalyzer.App/Compatibility/IsExternalInit.cs`

## 4. 検証結果

- `dotnet build` : 成功（0 Error / 0 Warning）
- `dotnet test` : 成功（7 Passed / 0 Failed）

## 5. 既知事項

- self-contained 配布は対象外（.NET Framework 4.6.2 ランタイムの事前導入が必要）
- 本作業では GUI の手動起動確認は未実施
