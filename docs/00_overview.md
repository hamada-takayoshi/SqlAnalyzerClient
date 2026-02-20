# SQL Analyzer Client – Overview

## 1. Purpose

SQL Analyzer Client is a Windows desktop application that:

- Formats SQL statements
- Analyzes a single SQL statement
- Extracts table references and relations (JOIN/APPLY)
- Extracts SELECT output items (SELECT only)
- Generates table relationship diagrams
- Exports results as Markdown and image files

The application runs completely offline.

---

## 2. Core Constraints (Must)

### 2.1 Offline-Only Runtime

At runtime, the application must:

- Not perform any network communication
- Not call external APIs
- Not connect to any database
- Not send telemetry

All processing must be local.

---

### 2.2 Target Dialect (v1)

- SQL Server (T-SQL)

The architecture must allow future extension to:
- Oracle
- Other SQL dialects

---

### 2.3 Supported SQL Types (v1)

Supported:

- SELECT
- INSERT
- UPDATE
- DELETE
- MERGE

Not supported:

- DDL (CREATE, ALTER, DROP, etc.)
- Stored procedure batch analysis
- Dynamic SQL reconstruction

If DDL is detected, a diagnostic must be generated.

---

### 2.4 Single Statement Rule

Only the first SQL statement in the input is analyzed.

Statement boundary is defined as:

- The first semicolon `;`
- OR the first `GO` batch separator (case-insensitive)

If additional SQL statements exist after the first boundary:

- They are ignored
- A diagnostic must be generated:
  - Code: MULTI_STATEMENT_TRUNCATED

---

## 3. Output

### 3.1 Analysis Result (Domain Model)

The analysis result must include:

- Statement type
- Table references
- Table relations (JOIN/APPLY)
- SELECT output items (if SELECT)
- Diagnostics

All outputs are represented using a dialect-agnostic domain model.

---

### 3.2 Diagram Output

The system must generate:

1. Mermaid diagram (Markdown text)
2. PNG image (generated locally)

The diagram must:

- Include table nodes
- Include directed edges
- Display JoinType labels

---

### 3.3 SELECT-Specific Output

For SELECT statements only:

- Output column name (alias or inferred)
- Expression text
- Source table (if resolvable)
- Logical name extracted from comments

If resolution fails, analysis continues.

---

## 4. Logical Name Extraction

Logical names must be extracted from comments placed immediately after:

- Table references
- SELECT expressions

Supported patterns:

1. Block comment:

   TABLEA /* Logical Name */

2. Line comment:

   TABLEA -- Logical Name

Whitespace variations must be tolerated.

---

## 5. Architecture Summary

The solution is structured into:

- SqlAnalyzer.Domain
  - Dialect-agnostic domain model

- SqlAnalyzer.SqlServer
  - SQL Server parser and formatter implementation

- SqlAnalyzer.App
  - WPF MVVM client application

The Domain layer must not depend on parser libraries.

---

## 6. Non-Goals (v1)

The following are explicitly out of scope:

- SQL execution
- Result set retrieval
- Schema discovery (PK/FK detection)
- DDL analysis
- Multi-file batch processing
- Live parsing while typing

---

## 7. High-Level Milestones

1. WPF UI skeleton + dummy analyzer
2. Single-statement boundary extraction
3. SQL Server parsing and relation extraction
4. SELECT item extraction and logical name extraction
5. Diagram generation and export
6. Self-contained packaging and offline verification
