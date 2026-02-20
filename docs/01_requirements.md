# Functional and Non-Functional Requirements

## 1. Scope

### 1.1 Supported SQL Type (v1)

The application supports only **DML statements**:

- SELECT
- INSERT
- UPDATE
- DELETE
- MERGE

DDL statements (CREATE, ALTER, DROP, etc.) are out of scope in v1.
If DDL is detected, the application must emit a diagnostic error or warning.

---

### 1.2 Single Statement Rule

Only the **first SQL statement** in the input is analyzed.

Statement boundary is defined as:

- The first semicolon `;`
- OR the first `GO` batch separator (case-insensitive)

If additional statements exist after the first boundary:

- They must be ignored
- A diagnostic warning must be generated:
  - Code: `MULTI_STATEMENT_TRUNCATED`

---

## 2. Functional Requirements

### 2.1 SQL Input

- Multi-line SQL text input
- File load (.sql, .txt)
- Clipboard paste
- SQL history (last N entries, configurable)

---

### 2.2 SQL Formatting

- SQL Server dialect formatting
- Configurable:
  - Indentation width
  - Uppercase keywords on/off
- Formatting errors must be reported as diagnostics

---

### 2.3 SQL Analysis (All DML)

The system must extract:

#### 2.3.1 Statement Type
- SELECT
- INSERT
- UPDATE
- DELETE
- MERGE
- Unknown

#### 2.3.2 Table References

For each table reference:

- Schema name (if present)
- Object name
- Alias (if present)
- Logical name (if comment exists)

Special cases:
- MERGE target and source
- INSERT target
- UPDATE target
- DELETE target

#### 2.3.3 Table Relations

Relations must be generated for:

- INNER JOIN
- LEFT OUTER JOIN
- RIGHT OUTER JOIN
- FULL OUTER JOIN
- CROSS JOIN
- CROSS APPLY
- OUTER APPLY

Relations must:
- Be directional (left-to-right as written)
- Store JoinType
- Not require ON condition storage in v1

CRUD classification is not required.

---

### 2.4 SELECT-Specific Analysis

Only when statement type is SELECT:

#### 2.4.1 Output Column List

For each SELECT item:

- Output name (alias or inferred)
- Expression text
- Column name (if resolvable)
- Source table (if resolvable)

If resolution fails:
- Mark as unresolved
- Do not fail analysis

---

#### 2.4.2 Logical Name Extraction

Logical names must be extracted from comments placed immediately after:

- Table references
- SELECT column expressions

Supported patterns:

1. Block comment  
   `TABLEA /* Logical Name */`

2. Line comment  
   `TABLEA -- Logical Name`

Whitespace variations must be tolerated.

Logical names must be stored in the domain model and displayed in UI.

---

### 2.5 Diagram Generation

The system must generate:

1. Mermaid diagram (Markdown text output)
2. PNG image generated locally (offline)

Diagram must include:

- Table nodes
- Directed edges
- JoinType labels

---

### 2.6 Diagnostics

Diagnostics must include:

- Severity (Info / Warning / Error)
- Code
- Message
- Source location (if available)

Required diagnostic codes:

- `MULTI_STATEMENT_TRUNCATED`
- `DDL_NOT_SUPPORTED`
- `UNSUPPORTED_SYNTAX`
- `PARTIAL_PARSE`

Analysis must be best-effort:
- Partial results may be returned even if diagnostics exist.

---

## 3. Non-Functional Requirements

### 3.1 Offline-Only Runtime

At runtime, the application must:

- Perform no network communication
- Not call any external APIs
- Not connect to any database
- Not send telemetry

All analysis must be local.

---

### 3.2 Distribution

- Windows x64
- Self-contained publish
- No runtime dependency on pre-installed .NET runtime

Publish command example:

- dotnet publish -c Release -r win-x64 --self-contained true


---

### 3.3 Performance

- Typical SQL (< 200 lines) must analyze within a few seconds
- Large SQL must support cancellation
- UI must not freeze during analysis

---

### 3.4 Extensibility

The architecture must allow future addition of:

- Oracle dialect
- Additional SQL dialects

Domain model must not depend on any parser library types.

---

## 4. Out of Scope (v1)

- SQL execution
- Result set retrieval
- Database schema discovery
- Stored procedure full-file parsing
- Dynamic SQL reconstruction
- DDL analysis

