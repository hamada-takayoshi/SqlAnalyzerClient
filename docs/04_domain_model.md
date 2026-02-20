# Domain Model Specification

This document defines the dialect-agnostic domain model used by the application.

The domain model MUST NOT depend on any SQL parser library types.
It represents normalized analysis results after parsing.

---

## 1. Design Principles

1. The domain layer is parser-independent.
2. Only one SQL statement is represented per analysis result.
3. All table references must be uniquely identifiable.
4. Relations reference tables by ID (not by name).
5. Partial parsing is allowed (best-effort model population).
6. The model must be JSON-serializable.

---

## 2. Root Model

### 2.1 SqlAnalysisResult

Represents the complete analysis output for one SQL statement.

Fields:
- `Dialect` (SqlDialect)
- `Document` (SqlDocumentInfo)
- `Statement` (SqlStatement)
- `Diagnostics` (list of Diagnostic)

### 2.2 SqlDocumentInfo

Fields:
- `Boundary` (StatementBoundary)
- `HasTrailingStatements` (bool)

### 2.3 StatementBoundary

Fields:
- `StartIndex` (int)
- `EndIndexExclusive` (int)
- `Kind` (BoundaryKind)

BoundaryKind:
- `Semicolon`
- `GoBatch`
- `EndOfText`
- `Unknown`

---

## 3. Statement Hierarchy

### 3.1 SqlStatement (abstract)

Fields:
- `StatementType` (SqlStatementType)
- `Tables` (list of TableRef)
- `Relations` (list of TableRelation)
- `Span` (optional SourceSpan)

SqlStatementType:
- `Select`
- `Insert`
- `Update`
- `Delete`
- `Merge`
- `Unknown`

### 3.2 SelectStatement : SqlStatement

Additional Fields:
- `SelectItems` (list of SelectItem)

### 3.3 InsertStatement : SqlStatement

Additional Fields:
- `Target` (TableRefId)
- `SourceKind` (InsertSourceKind)
- `TargetColumns` (optional list of string)

InsertSourceKind:
- `Values`
- `Select`
- `DefaultValues`
- `Unknown`

### 3.4 UpdateStatement : SqlStatement

Additional Fields:
- `Target` (TableRefId)
- `SetColumns` (optional list of string)

### 3.5 DeleteStatement : SqlStatement

Additional Fields:
- `Target` (TableRefId)

### 3.6 MergeStatement : SqlStatement

Additional Fields:
- `Target` (TableRefId)
- `Source` (TableSourceRef)
- `HasInsert` (bool?)
- `HasUpdate` (bool?)
- `HasDelete` (bool?)

---

## 4. Table Model

### 4.1 TableRefId

- String-based identifier
- Must be unique within a statement
- Recommended format: `"t1"`, `"t2"`, ...

### 4.2 TableRef

Fields:
- `Id` (TableRefId)
- `Source` (TableSourceRef)
- `Alias` (optional string)
- `LogicalName` (optional string)
- `RoleHints` (optional list of TableRoleHint)
- `Span` (optional SourceSpan)

TableRoleHint:
- `InsertTarget`
- `UpdateTarget`
- `DeleteTarget`
- `MergeTarget`
- `MergeSource`

### 4.3 TableSourceRef

Fields:
- `Kind` (TableSourceKind)
- `Name` (optional QualifiedName)
- `ExpressionText` (optional string)
- `Span` (optional SourceSpan)

TableSourceKind:
- `PhysicalTable`
- `DerivedTable`
- `Function`
- `Unknown`

### 4.4 QualifiedName

Fields:
- `Database` (optional string)
- `Schema` (optional string)
- `Object` (string)
- `Raw` (string)

`Raw` preserves original text including quoting.

---

## 5. Table Relations

### 5.1 TableRelation

Fields:
- `From` (TableRefId)
- `To` (TableRefId)
- `JoinType` (JoinType)
- `ConditionText` (optional string)
- `Span` (optional SourceSpan)

JoinType:
- `Inner`
- `LeftOuter`
- `RightOuter`
- `FullOuter`
- `Cross`
- `CrossApply`
- `OuterApply`

Relations must be directional:
- `FROM A LEFT JOIN B` -> `A -> B`

---

## 6. SELECT Items

### 6.1 SelectItem

Fields:
- `OutputName` (optional string)
- `ExpressionText` (string)
- `SourceColumn` (optional ColumnRef)
- `LogicalName` (optional string)
- `Span` (optional SourceSpan)

### 6.2 ColumnRef

Fields:
- `ColumnName` (string)
- `TableAliasOrName` (optional string)
- `ResolvedTable` (optional TableRefId)
- `Span` (optional SourceSpan)

If resolution fails, `ResolvedTable` remains null.

---

## 7. Diagnostics

### 7.1 Diagnostic

Fields:
- `Severity` (DiagnosticSeverity)
- `Code` (string)
- `Message` (string)
- `Span` (optional SourceSpan)

DiagnosticSeverity:
- `Info`
- `Warning`
- `Error`

Required codes:
- `MULTI_STATEMENT_TRUNCATED`
- `DDL_NOT_SUPPORTED`
- `UNSUPPORTED_SYNTAX`
- `PARTIAL_PARSE`

---

## 8. SourceSpan

Represents a location in the original SQL text.

Fields:
- `StartIndex` (int)
- `Length` (int)
- `StartLine` (optional int)
- `StartColumn` (optional int)

---

## 9. Invariants

1. All `TableRelation.From` and `TableRelation.To` must reference existing `TableRef.Id`.
2. `Statement.Tables` must contain all tables referenced by relations.
3. `JoinType` must always be a defined enum value.
4. Domain model must not contain parser-specific types.
5. Domain model must be serializable to JSON without custom converters.

---

## 10. Examples

### 10.1 Simple JOIN

Input:

```sql
SELECT *
FROM A
LEFT JOIN B ON A.Id = B.AId;
```

Conceptual result:
- StatementType = Select
- Tables = [A, B]
- Relations = [A -> B (LeftOuter)]

### 10.2 APPLY

Input:

```sql
SELECT *
FROM A
OUTER APPLY dbo.FN(A.Id) F;
```

Conceptual result:
- Tables = [A, dbo.FN(A.Id) as F]
- Relations = [A -> F (OuterApply)]


### 10.3 Multi-statement Truncation

Input:

```sql
SELECT 1;
SELECT 2;
```
Conceptual result:
- Only the first statement is analyzed.
- Diagnostic warning:
  - Code = MULTI_STATEMENT_TRUNCATED

---

## 11. Ownership

This domain model is the single source of truth for:

- UI display
- Diagram generation
- JSON export
- Future dialect implementations

