using System.Collections.Frozen;

using Netptune.Core.Constants;
using Netptune.Core.Enums;
using Netptune.Query.Compilation.Fields;
using Netptune.Query.Schema;
using Netptune.Query.Tasks.Fields;

namespace Netptune.Query.Tasks;

public sealed class TaskFieldCatalog : IQueryFieldCatalog
{
    public static TaskFieldCatalog Instance { get; } = new();

    public IReadOnlyList<QueryField> Fields { get; } =
    [
        new QueryField
        {
            Key = TaskFieldKeys.Name,
            Name = "Name",
            ValueType = QueryValueType.Text,
            Operators = QueryOperatorSets.Text,
            ParameterType = QueryParameterType.Text,
            Compiler = new TextFieldCompiler { Column = "pt.name" },
            SortKey = "name",
        },
        new QueryField
        {
            Key = TaskFieldKeys.Description,
            Name = "Description",
            ValueType = QueryValueType.Text,
            Operators = QueryOperatorSets.Text,
            ParameterType = QueryParameterType.Text,
            Compiler = new TextFieldCompiler { Column = "pt.description" },
        },
        new QueryField
        {
            Key = TaskFieldKeys.SystemId,
            Name = "System id",
            ValueType = QueryValueType.Text,
            Operators = QueryOperatorSets.Text,
            ParameterType = QueryParameterType.Text,
            Compiler = new TextFieldCompiler { Column = "CONCAT_WS('-', p.key, pt.project_scope_id::text)" },
            SortKey = "systemId",
        },
        new QueryField
        {
            Key = TaskFieldKeys.Status,
            Name = "Status",
            ValueType = QueryValueType.Enum,
            Operators = QueryOperatorSets.Enumeration,
            ParameterType = QueryParameterType.Integer,
            Compiler = new ScalarFieldCompiler { Column = "pt.status_id" },
            OptionSource = QueryOptionSources.Statuses,
            SortKey = "status",
        },
        new QueryField
        {
            Key = TaskFieldKeys.StatusCategory,
            Name = "Status category",
            ValueType = QueryValueType.Enum,
            Operators = QueryOperatorSets.Enumeration,
            ParameterType = QueryParameterType.Integer,
            Compiler = new ScalarFieldCompiler { Column = "st.category" },
            OptionSource = QueryOptionSources.StatusCategories,
            EnumType = typeof(StatusCategory),
        },
        new QueryField
        {
            Key = TaskFieldKeys.Priority,
            Name = "Priority",
            ValueType = QueryValueType.Enum,
            Operators = QueryOperatorSets.NullableEnumeration,
            ParameterType = QueryParameterType.Integer,
            Compiler = new ScalarFieldCompiler { Column = "pt.priority", MatchesNullOnZero = true },
            OptionSource = QueryOptionSources.Priorities,
            EnumType = typeof(TaskPriority),
            SortKey = "priority",
        },
        new QueryField
        {
            Key = TaskFieldKeys.EstimateType,
            Name = "Estimate type",
            ValueType = QueryValueType.Enum,
            Operators = QueryOperatorSets.NullableEnumeration,
            ParameterType = QueryParameterType.Integer,
            Compiler = new ScalarFieldCompiler { Column = "pt.estimate_type" },
            OptionSource = QueryOptionSources.EstimateTypes,
            EnumType = typeof(EstimateType),
        },
        new QueryField
        {
            Key = TaskFieldKeys.EstimateValue,
            Name = "Estimate",
            ValueType = QueryValueType.Number,
            Operators = QueryOperatorSets.Number,
            ParameterType = QueryParameterType.Decimal,
            Compiler = new ScalarFieldCompiler { Column = "pt.estimate_value" },
            SortKey = "estimateValue",
        },
        new QueryField
        {
            Key = TaskFieldKeys.Project,
            Name = "Project",
            ValueType = QueryValueType.Enum,
            Operators = QueryOperatorSets.NullableEnumeration,
            ParameterType = QueryParameterType.Integer,
            Compiler = new ScalarFieldCompiler { Column = "pt.project_id" },
            OptionSource = QueryOptionSources.Projects,
            SortKey = "projectName",
        },
        new QueryField
        {
            Key = TaskFieldKeys.Sprint,
            Name = "Sprint",
            ValueType = QueryValueType.Enum,
            Operators = QueryOperatorSets.NullableEnumeration,
            ParameterType = QueryParameterType.Integer,
            Compiler = new ScalarFieldCompiler { Column = "pt.sprint_id" },
            OptionSource = QueryOptionSources.Sprints,
            SortKey = "sprint",
        },
        new QueryField
        {
            Key = TaskFieldKeys.Owner,
            Name = "Owner",
            ValueType = QueryValueType.Enum,
            Operators = QueryOperatorSets.NullableEnumeration,
            ParameterType = QueryParameterType.Text,
            Compiler = new ScalarFieldCompiler { Column = "pt.owner_id" },
            OptionSource = QueryOptionSources.Members,
        },
        new QueryField
        {
            Key = TaskFieldKeys.Assignees,
            Name = "Assignees",
            ValueType = QueryValueType.Collection,
            Operators = QueryOperatorSets.Collection,
            ParameterType = QueryParameterType.Text,
            Compiler = new TaskAssigneeCompiler(),
            OptionSource = QueryOptionSources.Members,
            IsMultiValued = true,
            SortKey = "assignees",
        },
        new QueryField
        {
            Key = TaskFieldKeys.Tags,
            Name = "Tags",
            ValueType = QueryValueType.Collection,
            Operators = QueryOperatorSets.Collection,
            ParameterType = QueryParameterType.Text,
            Compiler = new TaskTagCompiler(),
            OptionSource = QueryOptionSources.Tags,
            IsMultiValued = true,
        },
        new QueryField
        {
            Key = TaskFieldKeys.StartDate,
            Name = "Start date",
            ValueType = QueryValueType.Date,
            Operators = QueryOperatorSets.Date,
            ParameterType = QueryParameterType.Date,
            Compiler = new ScalarFieldCompiler { Column = "pt.start_date" },
            SortKey = "startDate",
        },
        new QueryField
        {
            Key = TaskFieldKeys.DueDate,
            Name = "Due date",
            ValueType = QueryValueType.Date,
            Operators = QueryOperatorSets.DueDate,
            ParameterType = QueryParameterType.Date,
            Compiler = new ScalarFieldCompiler { Column = "pt.due_date" },
            SortKey = "dueDate",
        },
        new QueryField
        {
            Key = TaskFieldKeys.CreatedAt,
            Name = "Created",
            ValueType = QueryValueType.Timestamp,
            Operators = QueryOperatorSets.Date,
            ParameterType = QueryParameterType.Timestamp,
            Compiler = new TimestampFieldCompiler { Column = "pt.created_at" },
            SortKey = "createdAt",
        },
        new QueryField
        {
            Key = TaskFieldKeys.UpdatedAt,
            Name = "Updated",
            ValueType = QueryValueType.Timestamp,
            Operators = QueryOperatorSets.Date,
            ParameterType = QueryParameterType.Timestamp,
            Compiler = new TimestampFieldCompiler { Column = "pt.updated_at" },
            SortKey = "updatedAt",
        },
        new QueryField
        {
            Key = TaskFieldKeys.Flags,
            Name = "Flags",
            ValueType = QueryValueType.Collection,
            Operators = QueryOperatorSets.Presence,
            ParameterType = QueryParameterType.None,
            Compiler = new TaskFlagCompiler(),
            IsMultiValued = true,
        },
        new QueryField
        {
            Key = TaskFieldKeys.Comments,
            Name = "Comments",
            ValueType = QueryValueType.Collection,
            Operators = QueryOperatorSets.Presence,
            ParameterType = QueryParameterType.None,
            Compiler = new TaskCommentCompiler(),
            IsMultiValued = true,
        },
        new QueryField
        {
            Key = TaskFieldKeys.Relations,
            Name = "Relations",
            ValueType = QueryValueType.Collection,
            Operators = QueryOperatorSets.Collection,
            ParameterType = QueryParameterType.Text,
            ValueParser = new TaskRelationReferenceParser(),
            Compiler = new TaskRelationCompiler(),
            OptionSource = QueryOptionSources.RelationTypes,
            IsMultiValued = true,
        },
    ];

    private readonly FrozenDictionary<string, QueryField> FieldsByKey;

    private TaskFieldCatalog()
    {
        FieldsByKey = Fields.ToFrozenDictionary(field => field.Key, StringComparer.OrdinalIgnoreCase);
    }

    public QueryField? Find(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        FieldsByKey.TryGetValue(key.Trim(), out var field);

        return field;
    }
}
