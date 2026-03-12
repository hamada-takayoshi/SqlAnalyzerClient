# 関連図ラベル表示変更 作業サマリ（2026-03-13）

## 1. 対応目的

関連図（Mermaid / PNG）のテーブルノード表示を、従来の「別名優先」から  
「テーブル物理名 + 別名」へ変更する。

承認案:
- `スキーマ.物理名 (別名)` 形式（案2）

## 2. 変更内容

### 2.1 ラベル生成ロジックの変更

対象:
- `SqlAnalyzer.App/Services/DiagramService.cs`

変更点:
- `BuildNodeLabel(TableRef table)` を更新
  - 物理テーブル名が取得できる場合:
    - 別名あり: `schema.object (alias)` または `object (alias)`
    - 別名なし: `schema.object` または `object`
  - 物理テーブル名が取得できない場合:
    - 従来どおり `ExpressionText`、それもなければ `TableRef.Id` にフォールバック
- `BuildPhysicalTableName(QualifiedName? name)` を追加
  - `QualifiedName.Schema` と `QualifiedName.Object` から表示名を組み立て

## 3. 動作イメージ

- 変更前:
  - `u`
- 変更後:
  - `dbo.Users (u)`

## 4. 検証結果

- `dotnet build` 実行: 成功（0 Error, 5 Warning）
- `dotnet test` 実行: 成功（7 Passed, 0 Failed）

## 5. 影響範囲

- 関連図ノードの表示文字列のみ変更
- Domainモデル、解析結果構造、JOIN関係抽出ロジックには変更なし

## 6. 既知事項

- Nullable 警告が既存で残存（今回の変更前から存在する箇所を含む）
  - `SqlAnalyzer.SqlServer/Analysis/SqlStatementDomainMapper.cs`
  - `SqlAnalyzer.App/Services/DiagramService.cs`
