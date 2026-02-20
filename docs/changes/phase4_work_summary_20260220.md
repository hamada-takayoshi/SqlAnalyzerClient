# Phase 4 作業サマリ (2026-02-20)

## 1) 変更ファイル一覧

追加:
- `SqlAnalyzer.SqlServer/Parsing/ScriptDomParseResult.cs`
- `SqlAnalyzer.SqlServer/Parsing/ScriptDomSqlParser.cs`
- `SqlAnalyzer.SqlServer/Analysis/SqlServerAnalyzer.cs`
- `SqlAnalyzer.SqlServer/Analysis/SqlStatementDomainMapper.cs`
- `SqlAnalyzer.App/Verification/Phase4VerificationHarness.cs`
- `docs/changes/phase4_work_summary_20260220.md`

変更:
- `SqlAnalyzer.SqlServer/SqlAnalyzer.SqlServer.csproj`
- `SqlAnalyzer.App/MainWindow.xaml.cs`

削除:
- なし

## 2) 変更内容サマリ

- `SqlAnalyzer.SqlServer` に実アナライザーを実装し、ダミー利用から切り替え。
- ScriptDom 解析ラッパー（`ScriptDomSqlParser`）と解析結果モデルを追加。
- Phase 4 範囲として以下を Domain モデルへマッピング:
  - 文種判定: `Select`, `Insert`, `Update`, `Delete`, `Merge`
  - テーブル抽出（`t1`, `t2`, ... の一意ID）
  - JOIN/APPLY 関係抽出（方向付き）
- JoinType / APPLY マッピング:
  - `INNER JOIN` -> `JoinType.Inner`
  - `LEFT [OUTER] JOIN` -> `JoinType.LeftOuter`
  - `RIGHT [OUTER] JOIN` -> `JoinType.RightOuter`
  - `FULL [OUTER] JOIN` -> `JoinType.FullOuter`
  - `CROSS JOIN` -> `JoinType.Cross`
  - `CROSS APPLY` -> `JoinType.CrossApply`
  - `OUTER APPLY` -> `JoinType.OuterApply`
- DDL 検出（`Create*`, `Alter*`, `Drop*`, `Truncate*`）を追加し、`DDL_NOT_SUPPORTED` を返却。
- ベストエフォート診断:
  - パースエラーあり -> `PARTIAL_PARSE`
  - 未対応文種 -> `UNSUPPORTED_SYNTAX`
  - DDL -> `DDL_NOT_SUPPORTED`
  - 複数文切り捨ては Phase 3 境界抽出結果を引き継ぎ `MULTI_STATEMENT_TRUNCATED` を維持
- App 側接続:
  - `MainWindow` の analyzer を `SqlServerAnalyzer` に変更
  - 非同期/キャンセル（既存 `MainViewModel`）は維持

## 3) 検証ターゲット

1. SELECT + JOIN
- 入力:
  - `SELECT * FROM A LEFT JOIN B ON A.Id = B.AId;`
- 期待:
  - `StatementType = Select`、A/B テーブル、`A -> B (LeftOuter)`
- 実際:
  - 内部ハーネスで確認: PASS

2. APPLY
- 入力:
  - `SELECT * FROM A OUTER APPLY dbo.FN(A.Id) F;`
- 期待:
  - `OuterApply` 関係、F のテーブル情報取得
- 実際:
  - 内部ハーネスで確認: PASS

3. INSERT / UPDATE / DELETE / MERGE 認識
- 入力:
  - `INSERT INTO T1(Id) VALUES (1);`
  - `UPDATE T1 SET Name = 'X';`
  - `DELETE FROM T1;`
  - `MERGE INTO T1 AS T USING T2 AS S ON T.Id = S.Id WHEN MATCHED THEN UPDATE SET T.Name = S.Name;`
- 期待:
  - 文種判定が正しい、対象テーブルが含まれる
- 実際:
  - 内部ハーネスで確認: PASS

4. DDL 検出
- 入力:
  - `CREATE TABLE X (Id int);`
- 期待:
  - `DDL_NOT_SUPPORTED`
- 実際:
  - 内部ハーネスで確認: PASS

5. 複数文切り捨て継続
- 入力:
  - `SELECT 1; SELECT 2;`
- 期待:
  - `MULTI_STATEMENT_TRUNCATED`
- 実際:
  - 内部ハーネス + Phase 3 結合処理で確認: PASS

## 4) 実行コマンド

- `dotnet add .\SqlAnalyzer.SqlServer\SqlAnalyzer.SqlServer.csproj package Microsoft.SqlServer.TransactSql.ScriptDom`
- `dotnet restore`
- `dotnet build`
- `dotnet run --project .\SqlAnalyzer.App`

## 5) ビルド/実行結果

- Build: 成功（警告 0 / エラー 0）
- Run: 成功（`RUNNING_OK`、起動時ハーネス通過）

## 6) 残課題 / 未実装

次フェーズへ繰り越し（意図どおり）:
- Phase 5: SELECT 項目抽出・論理名抽出
- Phase 6: 図生成/エクスポート
- Phase 8: SQL フォーマット実装

現時点の既知事項:
- DDL 判定は型名プレフィックス判定を使用
- `PARTIAL_PARSE` は先頭エラー中心のメッセージ
- MERGE 関係は Phase 4 時点で `Inner` のプレースホルダ関係として表現
