# UI Design Specification

This document defines the WPF UI structure and interaction behavior.

The UI must follow MVVM architecture.
All displayed data must originate from the Domain model.

---

# 1. Main Window Layout

The main window consists of:

1. Top Command Bar
2. Left Pane: SQL Editor
3. Right Pane: Analysis Result Tabs

Layout style: Two-column grid (Left: SQL input, Right: Results)

---

# 2. Top Command Bar

Controls:

- Dialect Selector (ComboBox)
  - Default: SQLServer
  - Future-proof for additional dialects

- Buttons:
  - Format
  - Analyze
  - Cancel (enabled only during analysis)
  - Settings

- Status Area (right aligned):
  - Ready / Formatting... / Analyzing...
  - Execution time (optional)
  - Boundary info (e.g., "Analyzed until GO")

---

# 3. SQL Input Pane (Left)

## 3.1 SQL Editor

- Multi-line text editor
- Scrollable
- Monospace font (e.g., Consolas)
- Large input supported
- No network-based syntax highlighting

## 3.2 Additional Controls

Optional but recommended:

- Load from file (.sql, .txt)
- Save SQL to file
- Paste from clipboard
- SQL history dropdown (configurable size)

## 3.3 Single Statement Rule Notice

Display small informational text:

"Only the first SQL statement (terminated by ';' or GO) will be analyzed."

---

# 4. Result Tabs (Right Pane)

Use TabControl with the following tabs:

1. Summary
2. Tables
3. Relations
4. Select Items
5. Diagram
6. Diagnostics

---

# 5. Tab Details

---

## 5.1 Summary Tab

Display:

- Statement Type
- Number of Tables
- Number of Relations
- Number of Select Items (if SELECT)
- Number of Diagnostics
- Boundary Type (Semicolon / GO / EndOfText)

Optional:
- Quick export buttons

---

## 5.2 Tables Tab

Display a DataGrid with columns:

- Display Name (Alias if exists, else Object name)
- Schema
- Object
- Alias
- Logical Name

If RoleHints exist, optionally display:

- Role (InsertTarget, UpdateTarget, etc.)

Selecting a row may show detail panel (optional).

---

## 5.3 Relations Tab

Display a DataGrid with columns:

- From Table
- JoinType
- To Table

JoinType must display:

- Inner
- LeftOuter
- RightOuter
- FullOuter
- Cross
- CrossApply
- OuterApply

No CRUD classification.

---

## 5.4 Select Items Tab

If StatementType != Select:

- Show message:
  "This SQL statement is not a SELECT."

If SELECT:

Display DataGrid with columns:

- Output Name
- Expression
- Table (if resolved)
- Column Name (if resolved)
- Logical Name
- Resolution Status (Resolved / Unresolved)

Export buttons:
- Export as CSV
- Export as Markdown

---

## 5.5 Diagram Tab

Display area must support two modes:

Mode Selector:
- Diagram Image
- Mermaid Markdown

### Image Mode

- Display locally generated PNG
- Scrollable
- Zoom optional

### Mermaid Mode

- Display generated Mermaid text
- Copy to clipboard button

Export buttons:

- Save PNG
- Save Markdown

Diagram must include:

- Table nodes
- Directed edges
- JoinType label

---

## 5.6 Diagnostics Tab

Display list with:

- Severity (icon or text)
- Code
- Message
- Location (if available)

Clicking an item may:
- Navigate editor to source position (optional)

---

# 6. Settings Window

Settings window must include tabs:

1. Formatting
2. Logical Name Rules
3. Output
4. Runtime

---

## 6.1 Formatting Settings

- Indentation width
- Uppercase keywords (on/off)

---

## 6.2 Logical Name Rules

Enable/disable:

- Block comment pattern
- Line comment pattern

Whitespace tolerance must be enabled by default.

---

## 6.3 Output Settings

- Default output directory
- Default filename pattern
- Encoding (UTF-8 default)
- Default diagram format (PNG / Markdown)

---

## 6.4 Runtime Settings

- Analysis timeout (optional)
- Strict mode / Best-effort mode
- History size

---

# 7. UI Behavior Requirements

1. Analysis must be asynchronous.
2. UI must not freeze.
3. Cancel must stop analysis.
4. Diagnostics must display even if partial results exist.
5. All displayed data must be derived from Domain model.
6. No network calls at runtime.

---

# 8. MVVM Structure

Recommended ViewModels:

- MainViewModel
  - SqlText
  - SelectedDialect
  - FormatCommand
  - AnalyzeCommand
  - CancelCommand
  - AnalysisResult
  - Diagnostics

- TablesViewModel
- RelationsViewModel
- SelectItemsViewModel
- DiagramViewModel
- SettingsViewModel

Views must not contain parsing logic.

---

# 9. UI Non-Goals (v1)

- No drag-and-drop diagram editing
- No advanced graph filtering
- No live parsing while typing
- No database browsing
