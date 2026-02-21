# Phase 7 作業サマリ（20260221）

## 1) 変更ファイル一覧

### 追加
- `SqlAnalyzer.App/Verification/Phase7VerificationHarness.cs`
- `docs/changes/phase7_work_summary_20260221.md`

### 修正
- `SqlAnalyzer.App/ViewModels/MainViewModel.cs`
- `SqlAnalyzer.App/MainWindow.xaml.cs`

### 削除
- なし

## 2) 変更内容

### 実装した内容
- Phase7 要件の「必須診断コードの成立性」を固定するため、Phase7 検証ハーネスを追加しました。
- 起動時に Phase7 ハーネスを実行するよう接続しました。
- 解析実行時の予期しない例外に対するフォールバックを追加し、アプリがクラッシュせず `UNSUPPORTED_SYNTAX` 診断を返すようにしました。

### 診断パイプラインの整理
- 既存パイプライン:
  - `MULTI_STATEMENT_TRUNCATED` は App 層で境界抽出結果に基づき付与
  - `DDL_NOT_SUPPORTED` / `UNSUPPORTED_SYNTAX` / `PARTIAL_PARSE` は SqlServerAnalyzer で付与
- 追加対応:
  - 想定外例外時に UnknownStatement + `UNSUPPORTED_SYNTAX` を生成し、UI表示を継続

### Domain 影響
- Domain モデル変更なし（影響なし）

## 3) 検証対象

### Phase7VerificationHarness で確認した入力
1. `SELECT 1; SELECT 2;`
- 期待: `MULTI_STATEMENT_TRUNCATED`

2. `CREATE TABLE T (Id int);`
- 期待: `DDL_NOT_SUPPORTED`

3. `SELECT FROM;`
- 期待: `PARTIAL_PARSE`

4. `DECLARE @x int;`
- 期待: `UNSUPPORTED_SYNTAX`

5. 部分解析ケース（`SELECT FROM;`）
- 期待: モデル返却継続（クラッシュしない）

## 4) 実行コマンド

- `dotnet restore`
- `dotnet build`
- `dotnet run --project .\SqlAnalyzer.App`

## 5) build/run結果

- build: 成功（警告 0 / エラー 0）
- run: 成功（`RUNNING_OK`）

## 6) 未対応事項

- Phase8 の SQL フォーマット実装は未着手
- Phase9 の配布/パッケージング最適化は未着手
- 現在の診断はコード中心で、ユーザー向け文言の更なる詳細化は今後の改善余地あり
