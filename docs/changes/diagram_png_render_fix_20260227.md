# Diagram PNG黒画面不具合の原因特定と修正 (2026-02-27)

## 変更ファイル
- `SqlAnalyzer.App/Services/DiagramService.cs`

## 原因
`RenderPng` で `DrawingContext` を using宣言（`using DrawingContext dc = ...`）で開いたまま、
スコープ終端前に `RenderTargetBitmap.Render(visual)` を呼んでいた。

そのため描画内容が確定する前の `DrawingVisual` をレンダリングしてしまい、
`nonTransparent=0`（完全透明）PNGが出力されていた。

## 修正内容
`DrawingContext` を usingブロック化し、必ず `Dispose`（Close）後に `bitmap.Render(visual)` を実行する順序に変更。

## 検証
- `dotnet build SqlAnalyzerClient.sln` 成功
- `dotnet test SqlAnalyzerClient.sln` 成功（7 passed）

## 補足
デバッグログ計測コード（`DebugLog`）は引き続き残しており、再現時の確認に利用可能。
