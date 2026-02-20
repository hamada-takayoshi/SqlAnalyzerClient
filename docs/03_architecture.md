# Architecture Specification

This document defines the solution structure, responsibilities, and dependency rules.

The primary architectural goal is:
- Parser/Dialect implementations can evolve (SQL Server now, Oracle later)
- The WPF UI depends only on the Domain model and application-facing interfaces
- Runtime remains completely offline

---

## 1. Solution Structure

The repository contains these main projects:

1. SqlAnalyzer.Domain (Class Library)
2. SqlAnalyzer.SqlServer (Class Library)
3. SqlAnalyzer.App (WPF Application)

Optional future:
- SqlAnalyzer.Oracle (Class Library)

---

## 2. Responsibility Boundaries

### 2.1 SqlAnalyzer.Domain
Purpose:
- Owns all dialect-agnostic types used by UI and outputs.

Contains:
- Domain model records/enums (docs/04_domain_model.md)
- Diagnostic types
- No parsing logic
- No formatter logic
- No diagram generation logic

Rules:
- MUST NOT reference parser libraries (e.g., ScriptDom)
- MUST remain JSON-serializable without custom converters

---

### 2.2 SqlAnalyzer.SqlServer
Purpose:
- Implements SQL Server-specific parsing, analysis, and formatting.
- Transforms parser AST into Domain model.

Contains:
- Boundary extraction helper (shared logic may live here or in a shared utility)
- SQL Server parser/analyzer implementation
- SQL Server formatter implementation
- Mapping code for JOIN/APPLY -> JoinType enum
- SELECT item extraction and logical name extraction logic (SQL Server rules)

Rules:
- MAY reference SQL Server parsing libraries (e.g., Microsoft.SqlServer.TransactSql.ScriptDom)
- MUST NOT expose parser-specific types outside this project
- Outputs MUST be Domain model only

---

### 2.3 SqlAnalyzer.App
Purpose:
- WPF client application using MVVM
- Provides editor, analysis UI, diagram preview/export UI

Contains:
- Views (MainWindow, SettingsWindow)
- ViewModels (MainViewModel and sub-VMs)
- Application services for calling analyzers/formatters
- Local settings persistence
- Diagram generation and export (offline)

Rules:
- Must not contain SQL parsing logic
- Must not depend on ScriptDom
- Must not contain any network communication code

---

## 3. Key Interfaces (Application-facing)

To decouple UI from dialect implementations, define application-facing interfaces
in a UI-accessible place. Recommended options:

Option A (recommended for v1 simplicity):
- Define interfaces in SqlAnalyzer.SqlServer and consume directly in App
- Later refactor into a separate SqlAnalyzer.Abstractions project if needed

Option B (more scalable):
- Create SqlAnalyzer.Abstractions project for interfaces and shared utilities
- App references Abstractions + chosen dialect implementation projects

For v1, Option A is acceptable if rules are respected.

### 3.1 ISqlAnalyzer

Input:
- SqlDialect
- Raw SQL text

Output:
- SqlAnalysisResult (Domain)

### 3.2 ISqlFormatter

Input:
- SqlDialect
- Raw SQL text
- Formatting options (indent, uppercase keywords, etc.)

Output:
- Formatted SQL text
- Diagnostics (if formatting fails)

---

## 4. MVVM Model (App)

### 4.1 ViewModel Responsibilities

- ViewModels hold state and expose commands:
  - FormatCommand
  - AnalyzeCommand
  - CancelCommand
  - Export commands (diagram, select items)

- ViewModels must be thin:
  - They coordinate services and update UI state
  - They do not parse SQL

### 4.2 Services

App should contain services such as:
- AnalysisService (wraps analyzer call)
- DiagramService (Mermaid + PNG generation)
- ExportService (SaveFileDialog, writing files)
- SettingsService (local settings persistence)

---

## 5. Data Flow

1. User enters SQL in editor
2. User clicks Analyze
3. App performs:
   - Boundary extraction (single statement)
   - DDL detection (if present -> diagnostic)
   - Dialect analyzer call
4. Analyzer returns SqlAnalysisResult (Domain)
5. UI binds to Domain data
6. DiagramService generates Mermaid and PNG from Domain tables/relations
7. Export writes Mermaid/PNG/CSV to disk

---

## 6. Offline-Only Runtime Policy

At runtime:
- No HTTP/HTTPS or external calls
- No telemetry
- No DB connections
- All processing local

Implementation rules:
- Avoid adding HttpClient references
- Avoid WebView-based Mermaid rendering
- All diagram generation must be offline toolchain

---

## 7. Extensibility: Adding New Dialects

To add Oracle later:
- Implement an Oracle analyzer/formatter in a new project:
  - SqlAnalyzer.Oracle
- It must output the same Domain model:
  - SqlAnalysisResult
  - TableRef / TableRelation / SelectItem etc.

The App should:
- Allow dialect selection
- Route calls to the selected dialect implementation

---

## 8. Dependency Rules (Hard)

- App -> Domain
- SqlServer -> Domain
- App -> SqlServer (allowed)
- Domain -> (nothing)

Forbidden:
- Domain -> SqlServer
- Domain -> App
- App -> parser libraries directly

---

## 9. Incremental Implementation Strategy

Follow docs/13_task_plan.md:
- Start with UI + dummy analyzer
- Add boundary extraction
- Add real SQL Server parsing
- Add diagrams and export
- Add formatting
- Package as self-contained

---

## 10. Code Organization (Recommended)

SqlAnalyzer.App
- /Views
- /ViewModels
- /Services
- /Settings
- /Converters (optional)

SqlAnalyzer.SqlServer
- /Boundary
- /Parsing
- /Analysis
- /Formatting
- /CommentExtraction

SqlAnalyzer.Domain
- /Model
- /Diagnostics

---

This architecture ensures:
- Stable, testable domain model
- Dialect-specific logic isolated
- UI remains simple and offline-safe
