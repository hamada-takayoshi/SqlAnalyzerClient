# Phase11 作業サマリー (2026-02-27)

## 1. 変更ファイル一覧

### 追加
- `SqlAnalyzer.Tests/Tests/HarnessVerificationTests.cs`
- `SqlAnalyzer.Tests/Tests/Infrastructure/BoundaryExtractionHarness.cs`
- `SqlAnalyzer.Tests/Tests/Infrastructure/Phase5VerificationHarness.cs`
- `SqlAnalyzer.Tests/Tests/Infrastructure/Phase6VerificationHarness.cs`
- `SqlAnalyzer.Tests/Tests/Infrastructure/Phase7VerificationHarness.cs`
- `SqlAnalyzer.Tests/Tests/Infrastructure/Phase8VerificationHarness.cs`
- `SqlAnalyzer.Tests/Tests/TestData/` (ディレクトリ作成)
- `SqlAnalyzer.Tests/Tests/Assertions/` (ディレクトリ作成)

### 更新
- `SqlAnalyzer.App/MainWindow.xaml.cs`

### 削除
- `SqlAnalyzer.App/Verification/Phase5VerificationHarness.cs`
- `SqlAnalyzer.App/Verification/Phase6VerificationHarness.cs`
- `SqlAnalyzer.App/Verification/Phase7VerificationHarness.cs`
- `SqlAnalyzer.App/Verification/Phase8VerificationHarness.cs`
- `SqlAnalyzer.SqlServer/Boundary/BoundaryExtractionHarness.cs`

## 2. 変更内容

Phase11 の目的に合わせ、通常起動時に不要なハーネスコードを製品側プロジェクトからテストプロジェクトへ移設した。

- Step1 (棚卸し/分類)
  - A (Test-only): `Phase5/6/7/8VerificationHarness`, `BoundaryExtractionHarness`
  - B (Product-required): 該当なし
  - C (Shared-like but runtime不要): 上記ハーネス群（検証ロジック）
- Step2 (テスト構造準備)
  - `SqlAnalyzer.Tests/Tests/Infrastructure`
  - `SqlAnalyzer.Tests/Tests/TestData`
  - `SqlAnalyzer.Tests/Tests/Assertions`
- Step3 (コピーと参照切替)
  - ハーネスを `SqlAnalyzer.Tests/Tests/Infrastructure` にコピー
  - 名前空間をテスト側 (`SqlAnalyzer.Tests.Tests.Infrastructure`) に変更
  - `HarnessVerificationTests` を追加し、xUnit から `VerifyOrThrow()` を実行
- Step4 (ランタイム参照除去)
  - `MainWindow` コンストラクタからハーネス実行を削除
  - 不要 `using` を削除
- Step5 (製品側ハーネス削除)
  - App/SqlServer 側に残っていた元ハーネスファイルを削除

## 3. 検証対象

- `dotnet build` が成功すること
- `dotnet test` が成功すること
- アプリが起動できること（Visual Studio 起動相当の確認）
- 製品側からテスト専用ハーネス参照が除去されていること

## 4. 実行コマンド

1. `dotnet build SqlAnalyzerClient.sln`
2. `dotnet test SqlAnalyzerClient.sln`
3. `dotnet build SqlAnalyzerClient.sln` (ランタイム参照除去後)
4. `dotnet test SqlAnalyzerClient.sln` (ランタイム参照除去後)
5. `dotnet run --project SqlAnalyzer.App/SqlAnalyzer.App.csproj --no-build` を 5 秒起動確認後に停止
6. `dotnet build SqlAnalyzerClient.sln` (元ハーネス削除後)
7. `dotnet test SqlAnalyzerClient.sln` (元ハーネス削除後)
8. `dotnet run --project SqlAnalyzer.App/SqlAnalyzer.App.csproj --no-build` を 5 秒起動確認後に停止

## 5. build/test 結果

- 最終状態:
  - `dotnet build SqlAnalyzerClient.sln` 成功 (0 Error / 0 Warning)
  - `dotnet test SqlAnalyzerClient.sln` 成功 (7 passed / 0 failed)
- 起動確認:
  - `APP_STARTED_OK` を確認
- 補足:
  - 一度 `build` と `test` を並列実行した際に DLL ロック競合が発生したが、順次実行で解消

## 6. 追加ルール対応

- Domain モデルの変更なし
- NuGet 追加なし
- 依存方向は `Tests -> Product` のみを維持
- オフライン要件に影響する変更なし
- 公開 API の破壊的変更なし
- `InternalsVisibleTo` の追加・変更なし

## 7. 残課題

- なし
