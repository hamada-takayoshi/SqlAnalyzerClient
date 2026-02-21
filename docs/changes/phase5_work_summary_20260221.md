# Phase 5 作業サマリ（20260221）

## 1) 変更ファイル一覧

### 追加
- `SqlAnalyzer.App/Verification/Phase5VerificationHarness.cs`
- `docs/changes/phase5_work_summary_20260221.md`

### 修正
- `SqlAnalyzer.SqlServer/Analysis/SqlStatementDomainMapper.cs`
- `SqlAnalyzer.App/MainWindow.xaml.cs`

### 削除
- なし

## 2) 変更内容概要

### SELECT項目抽出方法
- `SqlStatementDomainMapper` に SELECT 項目抽出処理を追加しました。
- `SelectScalarExpression` と `SelectStarExpression` を対象に、`SelectItem` を生成します。
- `ExpressionText` は ScriptDom フラグメントの元 SQL 文字列から取得します。
- `OutputName` は以下の優先順で設定します。
  1. 明示エイリアス（`AS` / 別名）
  2. 推論（列参照なら列名）

### 論理名抽出仕様
- SELECT 式直後のコメントを論理名として抽出します。
- 対応形式:
  - ブロックコメント: `/* Logical Name */`
  - 行コメント: `-- Logical Name`
- 空白（スペース/タブ）は許容します。
- 式直後にコメントが無い場合は論理名なし（null）とします。

### 解決ロジックの考え方
- 列参照（例: `A.Id`）の場合、`ColumnRef` を作成します。
- 参照先テーブル解決は、解析済みテーブルの
  - エイリアス辞書
  - テーブル名辞書
  を使って解決します。
- 解決できた場合は `ResolvedTable` を設定し、`TableAliasOrName` には表示用に実テーブル名を設定します。
- 解決不能の場合は `ResolvedTable = null` のまま継続します。

### エラー耐性について
- SELECT 項目の一部が解決不能でも処理を継続します。
- 非SELECT文では `SelectItems` は空配列のままです。
- 例外でアプリが停止しないように、既存の解析フロー（ベストエフォート）に沿って実装しています。

## 3) 検証対象

使用した SQL 例（検証ハーネス）:

1. SELECT + 別名
```sql
SELECT A.Id AS UserId
FROM Users A;
```
想定結果:
- OutputName = `UserId`
- ExpressionText = `A.Id`
- SourceTable = `Users`

2. ブロックコメント論理名
```sql
SELECT A.Name /* User Name */
FROM Users A;
```
想定結果:
- LogicalName = `User Name`

3. 行コメント論理名
```sql
SELECT A.Name -- User Name
FROM Users A;
```
想定結果:
- LogicalName = `User Name`

4. 集計式 + 別名
```sql
SELECT COUNT(*) TotalCount
FROM Users;
```
想定結果:
- OutputName = `TotalCount`
- SourceTable = null

5. 非SELECT
```sql
UPDATE Users SET Name = 'X';
```
想定結果:
- SelectItems 空
- 解析がクラッシュしない

## 4) 実行コマンド

- `dotnet restore`
- `dotnet build`
- `dotnet run --project .\SqlAnalyzer.App`

## 5) build/run結果

- build: 成功（警告 0 / エラー 0）
- run: 成功（`RUNNING_OK`、起動時検証ハーネス通過）

## 6) 未対応事項

### 将来拡張ポイント
- 複雑式（CASE, サブクエリ, 関数ネスト）での出力名推論の高度化
- 複数スコープ/CTE/サブクエリを跨ぐ列解決の強化
- コメント紐付けルールの文脈対応（より厳密な位置判定）

### 制限事項
- Phase 5 範囲外の機能（図生成、フォーマット）は未実装です。
- 論理名抽出は「式直後コメント」に限定しています。
- 一部の曖昧な列参照は未解決（null）となる場合があります。
