# Implementation Task Plan

This document defines the phased implementation plan.
Codex must follow these steps in order.

Each phase must be completed and verified before moving to the next phase.

---

# Phase 0 – Solution Skeleton

## Goal

Create a working .NET 8 WPF solution with the required project structure.

## Required Projects

- SqlAnalyzer.Domain (Class Library)
- SqlAnalyzer.SqlServer (Class Library)
- SqlAnalyzer.App (WPF Application)

## Requirements

- Target: .NET 8
- Windows only
- No runtime network dependencies

## Definition of Done

- Solution builds successfully
- WPF app launches
- Main window opens

---

# Phase 1 – Domain Model Implementation

## Goal

Implement the domain model exactly as defined in:

docs/04_domain_model.md

## Requirements

- All enums
- All record types
- No parser library dependencies
- JSON serializable

## Definition of Done

- Domain project builds
- Domain types compile without warnings
- No references to parser libraries

---

# Phase 2 – UI Skeleton (No Real Parsing)

## Goal

Create the UI layout as defined in:

docs/05_ui_design.md

## Requirements

- Two-column layout (Editor / Result Tabs)
- Tabs: Summary, Tables, Relations, Select Items, Diagram, Diagnostics
- Buttons: Format, Analyze, Cancel, Settings
- Async Analyze command
- Dummy analyzer returns fixed test data

## Definition of Done

- Application runs
- Clicking Analyze populates all tabs with dummy domain data
- Diagnostics tab shows at least one example entry
- UI does not freeze

---

# Phase 3 – Single Statement Boundary Extraction

## Goal

Implement real boundary extraction logic.

Rules:
- First ';' OR first 'GO' (case-insensitive)
- Ignore trailing statements
- Emit MULTI_STATEMENT_TRUNCATED if extra content exists

## Definition of Done

- Input with multiple statements triggers diagnostic
- Only first statement is analyzed
- Boundary type displayed in Summary tab

---

# Phase 4 – SQL Server Parsing (Core)

## Goal

Replace dummy analyzer with real SQL Server parser implementation.

Recommended library:
- Microsoft.SqlServer.TransactSql.ScriptDom

## Requirements

- Extract statement type
- Extract table references
- Extract join/apply relations
- Populate domain model only
- No parser-specific types in domain

## Definition of Done

- SELECT/INSERT/UPDATE/DELETE/MERGE are recognized
- JOIN/APPLY types mapped correctly
- Tables and relations appear in UI
- No runtime network usage

---

# Phase 5 – SELECT Item Extraction

## Goal

Implement SELECT-specific analysis.

## Requirements

- Extract output columns
- Extract expression text
- Attempt table/column resolution
- Extract logical names from comments:
  - /* comment */
  - -- comment

Whitespace variations must be tolerated.

## Definition of Done

- Select Items tab populated correctly
- Logical names appear when comments exist
- Unresolved columns do not crash system

---

# Phase 6 – Diagram Generation

## Goal

Implement diagram export.

## Requirements

- Mermaid text generation
- PNG image generation (offline only)
- Directed edges with JoinType label

## Definition of Done

- Mermaid text displayed in Diagram tab
- PNG generated locally
- Export buttons work

---

# Phase 7 – Error Handling and Diagnostics

## Goal

Implement required diagnostics:

- MULTI_STATEMENT_TRUNCATED
- DDL_NOT_SUPPORTED
- UNSUPPORTED_SYNTAX
- PARTIAL_PARSE

## Definition of Done

- Diagnostics appear in Diagnostics tab
- Partial parse still returns usable model
- No crash on unsupported syntax

---

# Phase 8 – Formatting Implementation

## Goal

Implement SQL formatting for SQL Server.

## Requirements

- Indentation setting
- Uppercase keyword option
- Error reporting if formatting fails

## Definition of Done

- Format button updates editor content
- Formatting errors shown in Diagnostics

---

# Phase 9 – Self-Contained Packaging

## Goal

Verify offline-only distribution.

## Steps

1. Run:

   dotnet publish -c Release -r win-x64 --self-contained true

2. Copy published folder to offline environment
3. Verify:
   - App launches
   - No network calls
   - All features work

## Definition of Done

- App runs without installed .NET runtime
- No runtime internet dependency
- No exceptions on startup

---

# Phase 10 – Cleanup and Refactoring

## Goal

Improve structure and maintainability.

## Tasks

- Remove unused code
- Add XML comments
- Ensure consistent naming
- Verify no parser types leak into domain
- Review MVVM separation

---

# Implementation Rules for Codex

1. Do not skip phases.
2. Do not implement parser before boundary extraction.
3. Domain must remain parser-independent.
4. No network-related APIs allowed.
5. Each phase must build successfully before continuing.

---

# Future Phases (Out of Scope for v1)

- Oracle dialect
- Multi-file batch analysis
- Advanced graph filtering
- Real-time parsing
