using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlAnalyzer.Domain.Model;
using DomainDeleteStatement = SqlAnalyzer.Domain.Model.DeleteStatement;
using DomainInsertStatement = SqlAnalyzer.Domain.Model.InsertStatement;
using DomainMergeStatement = SqlAnalyzer.Domain.Model.MergeStatement;
using DomainSelectStatement = SqlAnalyzer.Domain.Model.SelectStatement;
using DomainUpdateStatement = SqlAnalyzer.Domain.Model.UpdateStatement;
using ScriptDomDeleteStatement = Microsoft.SqlServer.TransactSql.ScriptDom.DeleteStatement;
using ScriptDomInsertStatement = Microsoft.SqlServer.TransactSql.ScriptDom.InsertStatement;
using ScriptDomMergeStatement = Microsoft.SqlServer.TransactSql.ScriptDom.MergeStatement;
using ScriptDomSelectStatement = Microsoft.SqlServer.TransactSql.ScriptDom.SelectStatement;
using ScriptDomUpdateStatement = Microsoft.SqlServer.TransactSql.ScriptDom.UpdateStatement;

namespace SqlAnalyzer.SqlServer.Analysis;

internal sealed class SqlStatementDomainMapper
{
    private readonly string _sqlText;
    private readonly List<TableRef> _tables = [];
    private readonly List<TableRelation> _relations = [];
    private int _tableIndex;

    public SqlStatementDomainMapper(string sqlText)
    {
        _sqlText = sqlText;
    }

    public SqlStatement MapStatement(TSqlStatement statement)
    {
        return statement switch
        {
            ScriptDomSelectStatement select => MapSelect(select),
            ScriptDomInsertStatement insert => MapInsert(insert),
            ScriptDomUpdateStatement update => MapUpdate(update),
            ScriptDomDeleteStatement delete => MapDelete(delete),
            ScriptDomMergeStatement merge => MapMerge(merge),
            _ => new UnknownStatement()
        };
    }

    public bool IsDdlStatement(TSqlStatement statement)
    {
        string name = statement.GetType().Name;
        return name.StartsWith("Create", StringComparison.Ordinal) ||
               name.StartsWith("Alter", StringComparison.Ordinal) ||
               name.StartsWith("Drop", StringComparison.Ordinal) ||
               name.StartsWith("Truncate", StringComparison.Ordinal);
    }

    private DomainSelectStatement MapSelect(ScriptDomSelectStatement statement)
    {
        if (statement.QueryExpression is not null)
        {
            ExtractFromQueryExpression(statement.QueryExpression);
        }

        return new DomainSelectStatement
        {
            Tables = _tables.ToList(),
            Relations = _relations.ToList()
        };
    }

    private DomainInsertStatement MapInsert(ScriptDomInsertStatement statement)
    {
        TableRefId target = AddOrGetTable(statement.InsertSpecification?.Target, TableRoleHint.InsertTarget);

        InsertSourceKind sourceKind = InsertSourceKind.Unknown;
        InsertSource? source = statement.InsertSpecification?.InsertSource;
        switch (source)
        {
            case ValuesInsertSource:
                sourceKind = InsertSourceKind.Values;
                break;
            case SelectInsertSource selectInsert:
                sourceKind = InsertSourceKind.Select;
                if (selectInsert.Select is not null)
                {
                    ExtractFromQueryExpression(selectInsert.Select);
                }

                break;
            case ExecuteInsertSource:
                sourceKind = InsertSourceKind.Unknown;
                break;
            case null:
                sourceKind = InsertSourceKind.DefaultValues;
                break;
        }

        return new DomainInsertStatement
        {
            Target = target,
            SourceKind = sourceKind,
            Tables = _tables.ToList(),
            Relations = _relations.ToList()
        };
    }

    private DomainUpdateStatement MapUpdate(ScriptDomUpdateStatement statement)
    {
        TableRefId target = AddOrGetTable(statement.UpdateSpecification?.Target, TableRoleHint.UpdateTarget);

        if (statement.UpdateSpecification?.FromClause is not null)
        {
            foreach (TableReference tableReference in statement.UpdateSpecification.FromClause.TableReferences)
            {
                ProcessTableReference(tableReference);
            }
        }

        return new DomainUpdateStatement
        {
            Target = target,
            Tables = _tables.ToList(),
            Relations = _relations.ToList()
        };
    }

    private DomainDeleteStatement MapDelete(ScriptDomDeleteStatement statement)
    {
        TableRefId target = AddOrGetTable(statement.DeleteSpecification?.Target, TableRoleHint.DeleteTarget);

        if (statement.DeleteSpecification?.FromClause is not null)
        {
            foreach (TableReference tableReference in statement.DeleteSpecification.FromClause.TableReferences)
            {
                ProcessTableReference(tableReference);
            }
        }

        return new DomainDeleteStatement
        {
            Target = target,
            Tables = _tables.ToList(),
            Relations = _relations.ToList()
        };
    }

    private DomainMergeStatement MapMerge(ScriptDomMergeStatement statement)
    {
        TableRefId target = AddOrGetTable(statement.MergeSpecification?.Target, TableRoleHint.MergeTarget);
        TableSourceRef sourceRef = new();

        if (statement.MergeSpecification?.TableReference is not null)
        {
            TableRefId sourceId = AddOrGetTable(statement.MergeSpecification.TableReference, TableRoleHint.MergeSource);
            sourceRef = _tables.FirstOrDefault(t => t.Id == sourceId)?.Source ?? sourceRef;
            AddRelation(target, sourceId, JoinType.Inner, statement.MergeSpecification.SearchCondition);
        }

        bool? hasInsert = null;
        bool? hasUpdate = null;
        bool? hasDelete = null;

        if (statement.MergeSpecification?.ActionClauses is not null)
        {
            hasInsert = statement.MergeSpecification.ActionClauses.Any(a => a.Action is InsertMergeAction);
            hasUpdate = statement.MergeSpecification.ActionClauses.Any(a => a.Action is UpdateMergeAction);
            hasDelete = statement.MergeSpecification.ActionClauses.Any(a => a.Action is DeleteMergeAction);
        }

        return new DomainMergeStatement
        {
            Target = target,
            Source = sourceRef,
            HasInsert = hasInsert,
            HasUpdate = hasUpdate,
            HasDelete = hasDelete,
            Tables = _tables.ToList(),
            Relations = _relations.ToList()
        };
    }

    private void ExtractFromQueryExpression(QueryExpression? queryExpression)
    {
        if (queryExpression is null)
        {
            return;
        }

        switch (queryExpression)
        {
            case QuerySpecification querySpecification:
                if (querySpecification.FromClause is not null)
                {
                    foreach (TableReference tableReference in querySpecification.FromClause.TableReferences)
                    {
                        ProcessTableReference(tableReference);
                    }
                }

                break;
            case BinaryQueryExpression binary:
                ExtractFromQueryExpression(binary.FirstQueryExpression);
                ExtractFromQueryExpression(binary.SecondQueryExpression);
                break;
            case QueryParenthesisExpression parenthesized:
                ExtractFromQueryExpression(parenthesized.QueryExpression);
                break;
        }
    }

    private TableRefId ProcessTableReference(TableReference? tableReference)
    {
        if (tableReference is null)
        {
            return new TableRefId("t0");
        }

        switch (tableReference)
        {
            case QualifiedJoin qualifiedJoin:
            {
                TableRefId leftId = ProcessTableReference(qualifiedJoin.FirstTableReference);
                TableRefId rightId = ProcessTableReference(qualifiedJoin.SecondTableReference);
                AddRelation(leftId, rightId, MapJoinType(qualifiedJoin.QualifiedJoinType), qualifiedJoin.SearchCondition);
                return rightId;
            }
            case UnqualifiedJoin unqualifiedJoin:
            {
                TableRefId leftId = ProcessTableReference(unqualifiedJoin.FirstTableReference);
                TableRefId rightId = ProcessTableReference(unqualifiedJoin.SecondTableReference);
                AddRelation(leftId, rightId, MapJoinType(unqualifiedJoin.UnqualifiedJoinType), null);
                return rightId;
            }
            case JoinParenthesisTableReference joinParenthesis:
                return ProcessTableReference(joinParenthesis.Join);
            case NamedTableReference namedTable:
                return AddOrGetTable(namedTable, null);
            case SchemaObjectFunctionTableReference functionTable:
                return AddOrGetTable(functionTable, null);
            case QueryDerivedTable derivedTable:
                return AddOrGetTable(derivedTable, null);
            case VariableTableReference variableTable:
                return AddOrGetTable(variableTable, null);
            default:
                return AddOrGetTable(tableReference, null);
        }
    }

    private TableRefId AddOrGetTable(TableReference? tableReference, TableRoleHint? roleHint)
    {
        if (tableReference is null)
        {
            return new TableRefId("t0");
        }

        string idValue = $"t{++_tableIndex}";
        TableRefId id = new(idValue);

        string? alias = tableReference switch
        {
            NamedTableReference named => named.Alias?.Value,
            SchemaObjectFunctionTableReference func => func.Alias?.Value,
            QueryDerivedTable derived => derived.Alias?.Value,
            VariableTableReference variable => variable.Alias?.Value,
            _ => null
        };

        List<TableRoleHint>? roleHints = null;
        if (roleHint.HasValue)
        {
            roleHints = [roleHint.Value];
        }

        TableSourceRef source = CreateSource(tableReference);

        _tables.Add(new TableRef
        {
            Id = id,
            Source = source,
            Alias = alias,
            RoleHints = roleHints
        });

        if (tableReference is QueryDerivedTable queryDerivedTable)
        {
            ExtractFromQueryExpression(queryDerivedTable.QueryExpression);
        }

        return id;
    }

    private TableSourceRef CreateSource(TableReference tableReference)
    {
        return tableReference switch
        {
            NamedTableReference namedTable => new TableSourceRef
            {
                Kind = TableSourceKind.PhysicalTable,
                Name = ToQualifiedName(namedTable.SchemaObject)
            },
            SchemaObjectFunctionTableReference function => new TableSourceRef
            {
                Kind = TableSourceKind.Function,
                Name = function.SchemaObject is null ? null : ToQualifiedName(function.SchemaObject),
                ExpressionText = GetFragmentText(function)
            },
            QueryDerivedTable derived => new TableSourceRef
            {
                Kind = TableSourceKind.DerivedTable,
                ExpressionText = GetFragmentText(derived)
            },
            _ => new TableSourceRef
            {
                Kind = TableSourceKind.Unknown,
                ExpressionText = GetFragmentText(tableReference)
            }
        };
    }

    private static QualifiedName? ToQualifiedName(SchemaObjectName? schemaObjectName)
    {
        if (schemaObjectName is null || schemaObjectName.Identifiers.Count == 0)
        {
            return null;
        }

        IList<Identifier> identifiers = schemaObjectName.Identifiers;
        string raw = string.Join(".", identifiers.Select(i => i.QuoteType switch
        {
            QuoteType.SquareBracket => $"[{i.Value}]",
            QuoteType.DoubleQuote => $"\"{i.Value}\"",
            _ => i.Value
        }));

        string obj = identifiers[^1].Value;
        string? schema = identifiers.Count >= 2 ? identifiers[^2].Value : null;
        string? database = identifiers.Count >= 3 ? identifiers[^3].Value : null;

        return new QualifiedName
        {
            Database = database,
            Schema = schema,
            Object = obj,
            Raw = raw
        };
    }

    private void AddRelation(TableRefId from, TableRefId to, JoinType joinType, BooleanExpression? searchCondition)
    {
        _relations.Add(new TableRelation
        {
            From = from,
            To = to,
            JoinType = joinType,
            ConditionText = searchCondition is null ? null : GetFragmentText(searchCondition)
        });
    }

    private string? GetFragmentText(TSqlFragment? fragment)
    {
        if (fragment is null || fragment.StartOffset < 0 || fragment.FragmentLength <= 0)
        {
            return null;
        }

        if (fragment.StartOffset + fragment.FragmentLength > _sqlText.Length)
        {
            return null;
        }

        return _sqlText.Substring(fragment.StartOffset, fragment.FragmentLength);
    }

    private static JoinType MapJoinType(QualifiedJoinType joinType)
    {
        return joinType switch
        {
            QualifiedJoinType.Inner => JoinType.Inner,
            QualifiedJoinType.LeftOuter => JoinType.LeftOuter,
            QualifiedJoinType.RightOuter => JoinType.RightOuter,
            QualifiedJoinType.FullOuter => JoinType.FullOuter,
            _ => JoinType.Inner
        };
    }

    private static JoinType MapJoinType(UnqualifiedJoinType joinType)
    {
        return joinType switch
        {
            UnqualifiedJoinType.CrossJoin => JoinType.Cross,
            UnqualifiedJoinType.CrossApply => JoinType.CrossApply,
            UnqualifiedJoinType.OuterApply => JoinType.OuterApply,
            _ => JoinType.Cross
        };
    }
}
