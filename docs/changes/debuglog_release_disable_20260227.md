# DebugログのRelease無効化 修正サマリー (2026-02-27)

## 1. 変更ファイル一覧

### 更新
- `SqlAnalyzer.App/Services/DebugLog.cs`

### 追加
- `docs/changes/debuglog_release_disable_20260227.md` (本ファイル)

## 2. 変更内容

今回追加したデバッグ出力 (`DebugLog.Write`) がリリースビルドで出力されないように修正した。

- `DebugLog.Write` メソッドに `[Conditional("DEBUG")]` を付与
- これにより、`DEBUG` シンボルが未定義のビルド（Release）では呼び出しコードがコンパイル時に除去される

## 3. 目的

- Release 実行時にデバッグログファイル (`diagram_debug.log`) を生成しない
- 本番挙動にデバッグ用途の副作用を持ち込まない

## 4. 検証対象

- ソリューションがビルドできること
- 既存テストが通過すること

## 5. 実行コマンド

1. `dotnet build SqlAnalyzerClient.sln`
2. `dotnet test SqlAnalyzerClient.sln`

## 6. build/test 結果

- `dotnet build` 成功（0 Warning / 0 Error）
- `dotnet test` 成功（7 passed / 0 failed）

## 7. 影響範囲

- 影響は `DebugLog` 呼び出し経路に限定
- Domain モデル・解析ロジック・UI機能仕様には変更なし
- NuGet 追加なし

## 8. 残件

- なし
