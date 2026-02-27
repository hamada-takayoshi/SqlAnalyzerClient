# 起動不要要素の分析と整理 作業サマリー (2026-02-27)

## 1. 変更ファイル一覧

### 更新
- `SqlAnalyzer.App/ViewModels/MainViewModel.cs`

### 削除
- `SqlAnalyzer.SqlServer/Analysis/DummyAnalyzer.cs`

### 追加
- `docs/changes/phase11_startup_unnecessary_review_20260227.md` (本ファイル)

## 2. 実施内容

指定ドキュメント（`AGENTS.md`, `docs/00_overview.md`, `docs/01_requirements.md`, `docs/03_architecture.md`, `docs/04_domain_model.md`, `docs/05_ui_design.md`, `docs/13_task_plan.md`）を確認し、
「通常起動・通常実行に不要な要素」を製品コード（非テスト）で調査した。

承認済みの2項目のみを反映:

1. `SqlAnalyzer.SqlServer.Analysis.DummyAnalyzer` を削除
   - 理由: 起動経路および通常機能から未参照のため（定義のみ存在）。

2. `MainViewModel` の未使用 `DiagramImageText` 関連コードを削除
   - 削除対象: フィールド、プロパティ、`ApplyResult` 内の代入。
   - 理由: XAMLバインディングがなく、ViewModel内でも未利用。

## 3. 検証対象

- ソリューションがビルドできること
- テストが通過すること
- アプリが通常起動できること

## 4. 実行コマンド

1. `dotnet build SqlAnalyzerClient.sln`
2. `dotnet test SqlAnalyzerClient.sln`
3. `dotnet run --project SqlAnalyzer.App/SqlAnalyzer.App.csproj --no-build`（5秒起動確認後停止）

## 5. build/test/起動結果

- `dotnet build` 成功（0 Warning / 0 Error）
- `dotnet test` 成功（7 passed / 0 failed）
- 起動確認結果: `APP_STARTED_OK`

## 6. 制約遵守

- Domain モデル変更なし
- NuGet 追加なし
- オフライン要件に影響する変更なし
- 製品→テスト依存の逆流なし

## 7. 残件

- 今回承認された項目は完了
- 追加で同種の不要要素候補は現時点で確認なし
