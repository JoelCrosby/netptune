using Netptune.Core.Constants;
using Netptune.Query.Model;
using Netptune.Query.Schema;

namespace Netptune.Query.Validation;

public static class QueryValidator
{
    public static QueryValidationResult Validate(IQueryFieldCatalog catalog, QueryGroup? group)
    {
        if (group is null)
        {
            return QueryValidationResult.Valid;
        }

        var errors = new List<QueryValidationError>();

        ValidateGroup(catalog, group, "query", 1, errors);

        var conditionCount = group.CountConditions();

        if (conditionCount > ConditionGroupLimits.MaximumConditionCount)
        {
            errors.Add(new QueryValidationError
            {
                Path = "query",
                Message = $"A query cannot have more than {ConditionGroupLimits.MaximumConditionCount} conditions.",
            });
        }

        return new QueryValidationResult { Errors = errors };
    }

    private static void ValidateGroup(
        IQueryFieldCatalog catalog,
        QueryGroup group,
        string path,
        int depth,
        List<QueryValidationError> errors)
    {
        if (!Enum.IsDefined(group.Operator))
        {
            errors.Add(new QueryValidationError
            {
                Path = path,
                Message = $"Group operator '{(int)group.Operator}' is not supported.",
            });
        }

        if (depth > ConditionGroupLimits.MaximumDepth)
        {
            errors.Add(new QueryValidationError
            {
                Path = path,
                Message = $"Groups cannot be nested more than {ConditionGroupLimits.MaximumDepth} levels.",
            });

            return;
        }

        for (var index = 0; index < group.Conditions.Count; index++)
        {
            ValidateCondition(catalog, group.Conditions[index], $"{path}.conditions[{index}]", errors);
        }

        for (var index = 0; index < group.Groups.Count; index++)
        {
            ValidateGroup(catalog, group.Groups[index], $"{path}.groups[{index}]", depth + 1, errors);
        }
    }

    private static void ValidateCondition(
        IQueryFieldCatalog catalog,
        QueryCondition condition,
        string path,
        List<QueryValidationError> errors)
    {
        var field = catalog.Find(condition.Field);

        if (field is null)
        {
            errors.Add(new QueryValidationError
            {
                Path = path,
                Field = condition.Field,
                Message = $"Field '{condition.Field}' is not a known task field.",
            });

            return;
        }

        if (!Enum.IsDefined(condition.Operator))
        {
            errors.Add(new QueryValidationError
            {
                Path = path,
                Field = field.Key,
                Message = $"Operator '{(int)condition.Operator}' is not supported.",
            });

            return;
        }

        if (!field.Supports(condition.Operator))
        {
            errors.Add(new QueryValidationError
            {
                Path = path,
                Field = field.Key,
                Message = $"Operator '{condition.Operator}' cannot be used with '{field.Name}'.",
            });

            return;
        }

        var arityError = ValidateArity(field, condition, path);

        if (arityError is not null)
        {
            errors.Add(arityError);

            return;
        }

        ValidateValues(field, condition, path, errors);
    }

    private static QueryValidationError? ValidateArity(QueryField field, QueryCondition condition, string path)
    {
        var expected = QueryOperatorArity.For(condition.Operator);
        var actual = condition.Values.Count;
        var isSatisfied = QueryOperatorArity.IsSatisfiedBy(expected, actual);

        if (isSatisfied)
        {
            return null;
        }

        var requirement = expected switch
        {
            QueryArity.None => "no values",
            QueryArity.One => "exactly one value",
            QueryArity.Two => "exactly two values",
            _ => "at least one value",
        };

        return new QueryValidationError
        {
            Path = path,
            Field = field.Key,
            Message = $"Operator '{condition.Operator}' on '{field.Name}' requires {requirement}.",
        };
    }

    private static void ValidateValues(
        QueryField field,
        QueryCondition condition,
        string path,
        List<QueryValidationError> errors)
    {
        var isRelativeDays = condition.Operator is QueryOperator.InNextDays or QueryOperator.InLastDays;

        if (isRelativeDays)
        {
            var isValidDayCount = QueryValueBinder.TryParseDayCount(condition.Values[0], out _);

            if (!isValidDayCount)
            {
                errors.Add(new QueryValidationError
                {
                    Path = path,
                    Field = field.Key,
                    Message = $"'{condition.Values[0]}' is not a valid number of days.",
                });
            }

            return;
        }

        foreach (var value in condition.Values)
        {
            var isParsable = QueryValueBinder.TryParse(field, value, out _);

            if (!isParsable)
            {
                errors.Add(new QueryValidationError
                {
                    Path = path,
                    Field = field.Key,
                    Message = $"'{value}' is not a valid {field.ValueType.ToString().ToLowerInvariant()} value for '{field.Name}'.",
                });
            }
        }
    }
}
