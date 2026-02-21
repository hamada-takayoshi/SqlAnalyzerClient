# Phase 6 作業サマリ（20260221）

## 1) 変更ファイル一覧（追加/修正/削除）

### 追加
- `SqlAnalyzer.App/Services/DiagramService.cs`
- `SqlAnalyzer.App/Services/ExportService.cs`
- `SqlAnalyzer.App/Verification/Phase6VerificationHarness.cs`
- `docs/changes/phase6_work_summary_20260221.md`

### 修正
- `SqlAnalyzer.App/ViewModels/MainViewModel.cs`
- `SqlAnalyzer.App/MainWindow.xaml`
- `SqlAnalyzer.App/MainWindow.xaml.cs`

### 削除
- なし

## 2) 変更内容（Mermaid生成仕様、PNG生成方式、JoinType表現、UI追加点）

### Mermaid生成仕様
- 入力は Domain モデルのみ（`Statement.Tables`, `Statement.Relations`, `JoinType`）を使用。
- 先頭行は `flowchart LR`。
- ノードはテーブルごとに1つ生成。
  - 表示名は「Alias があれば Alias、なければ Object 名、なければ式/ID」。
- エッジは `From -> To` の方向で生成し、ラベルは `JoinType` 文字列を使用。
- ノード順・エッジ順はソートして決定し、出力を安定化（決定的出力）。

### PNG生成方式
- 追加パッケージなし、WPF の `DrawingVisual` + `RenderTargetBitmap` + `PngBitmapEncoder` でローカル生成。
- 完全オフラインで生成（WebView/HTTP/外部ツール不使用）。
- ノードをグリッド状に配置し、矢印付きエッジと JoinType ラベルを描画。

### JoinType表現
- MermaidエッジラベルとPNGエッジラベルの双方で、Domain の `JoinType` 名（`Inner`, `LeftOuter`, `CrossApply` など）をそのまま表示。

### UI追加点
- Diagram タブに以下を実装。
  - Mermaid モード:
    - Mermaid テキスト表示
    - Copy to Clipboard ボタン
    - Save Markdown ボタン（UTF-8で保存）
  - Image モード:
    - 生成PNGプレビュー表示
    - Save PNG ボタン
- PNG生成失敗時はクラッシュせず、Diagram タブにエラーメッセージ表示。

## 3) 検証対象（確認したSQL例と確認観点）

1. Simple JOIN
```sql
SELECT *
FROM A
LEFT JOIN B ON A.Id = B.AId;
```
- 確認観点: ノード2個、エッジ1本、`LeftOuter` 表示、PNG生成

2. APPLY
```sql
SELECT *
FROM A
OUTER APPLY dbo.FN(A.Id) F;
```
- 確認観点: `OuterApply` ラベル表示、PNG生成

3. 関係なし（単一テーブル）
```sql
SELECT *
FROM SingleTable;
```
- 確認観点: ノードのみ、エッジなし、PNG生成

4. 複数関係
```sql
SELECT *
FROM A
INNER JOIN B ON A.Id = B.AId
LEFT JOIN C ON B.Id = C.BId;
```
- 確認観点: Mermaid出力順の安定性、PNG生成

## 4) 実行コマンド（dotnet restore/build/run、必要ならテスト）

- `dotnet restore`
- `dotnet build`
- `dotnet run --project .\SqlAnalyzer.App`

## 5) build/run結果

- build: 成功（0 warnings / 0 errors）
- run: 成功（`RUNNING_OK`、起動時検証ハーネス通過）

## 6) 未対応事項（Phase7/8/9へ回す内容、既知の制限）

### Phase7/8/9へ回す内容
- Phase 7: 診断体系の拡張（Diagram生成失敗を Diagnostics タブへ統合する等）
- Phase 8: SQLフォーマット実装
- Phase 9: 配布・パッケージング最適化

### 既知の制限
- PNGレイアウトはシンプルな固定アルゴリズム（大規模グラフで重なり最適化は限定的）。
- Diagram エラーメッセージは Diagram タブ内表示に留まり、現時点では Diagnostics タブと連携していない。
- ノードラベルは要件に合わせ Alias 優先で表示しており、補足情報の表示は最小限。
