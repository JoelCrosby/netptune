import {
  QueryBuilderCatalog,
  QueryBuilderCondition,
  QueryBuilderField,
  QueryBuilderGroup,
  QueryBuilderGroupOperator,
  QueryBuilderInputType,
  QueryBuilderOperator,
  QueryBuilderOption,
} from '@shared/components/query-builder/query-builder.models';
import {
  acceptsManyValues,
  isRelativeDayOperator,
  operatorArity,
  taskQueryOperatorLabels,
} from './task-query-copy';
import {
  TaskQueryCatalog,
  TaskQueryCondition,
  TaskQueryField,
  TaskQueryGroup,
  TaskQueryGroupOperator,
  TaskQueryOperator,
  TaskQueryValueType,
} from './task-view.models';

// Projects the server's task-query catalog onto the shared builder vocabulary. The operator enum
// travels as a name rather than its number so a query in mid-edit stays readable, and so the
// builder never has to know that saved views number their operators at all.
const operatorKeys: Record<TaskQueryOperator, string> = {
  [TaskQueryOperator.equals]: 'equals',
  [TaskQueryOperator.notEquals]: 'notEquals',
  [TaskQueryOperator.in]: 'in',
  [TaskQueryOperator.notIn]: 'notIn',
  [TaskQueryOperator.contains]: 'contains',
  [TaskQueryOperator.notContains]: 'notContains',
  [TaskQueryOperator.startsWith]: 'startsWith',
  [TaskQueryOperator.isEmpty]: 'isEmpty',
  [TaskQueryOperator.isNotEmpty]: 'isNotEmpty',
  [TaskQueryOperator.greaterThan]: 'greaterThan',
  [TaskQueryOperator.greaterThanOrEqual]: 'greaterThanOrEqual',
  [TaskQueryOperator.lessThan]: 'lessThan',
  [TaskQueryOperator.lessThanOrEqual]: 'lessThanOrEqual',
  [TaskQueryOperator.between]: 'between',
  [TaskQueryOperator.inNextDays]: 'inNextDays',
  [TaskQueryOperator.inLastDays]: 'inLastDays',
  [TaskQueryOperator.isOverdue]: 'isOverdue',
};

const operatorsByKey = new Map<string, TaskQueryOperator>(
  Object.entries(operatorKeys).map(([operator, key]) => {
    return [key, Number(operator) as TaskQueryOperator];
  })
);

const dayUnit = $localize`:Unit after a relative day count in a query summary:days`;

export function toBuilderCatalog(
  catalog: TaskQueryCatalog,
  optionsFor: (field: TaskQueryField) => QueryBuilderOption[]
): QueryBuilderCatalog {
  return {
    fields: catalog.fields.map((field) => toBuilderField(field, optionsFor)),
    maximumDepth: catalog.maximumDepth,
    maximumConditionCount: catalog.maximumConditionCount,
  };
}

export function toBuilderGroup(group: TaskQueryGroup): QueryBuilderGroup {
  return {
    operator: group.operator as number as QueryBuilderGroupOperator,
    conditions: group.conditions.map((condition) => ({
      field: condition.field,
      operator: operatorKeys[condition.operator],
      values: condition.values,
    })),
    groups: group.groups.map(toBuilderGroup),
  };
}

export function fromBuilderGroup(group: QueryBuilderGroup): TaskQueryGroup {
  return {
    operator: group.operator as number as TaskQueryGroupOperator,
    conditions: group.conditions.map(fromBuilderCondition),
    groups: group.groups.map(fromBuilderGroup),
  };
}

function fromBuilderCondition(
  condition: QueryBuilderCondition
): TaskQueryCondition {
  return {
    field: condition.field,
    operator:
      operatorsByKey.get(condition.operator) ?? TaskQueryOperator.equals,
    values: condition.values,
  };
}

function toBuilderField(
  field: TaskQueryField,
  optionsFor: (field: TaskQueryField) => QueryBuilderOption[]
): QueryBuilderField {
  return {
    key: field.key,
    name: field.name,
    inputType: inputTypeFor(field.valueType),
    operators: field.operators.map(toBuilderOperator),
    options: optionsFor(field),
  };
}

function toBuilderOperator(operator: TaskQueryOperator): QueryBuilderOperator {
  const descriptor: QueryBuilderOperator = {
    key: operatorKeys[operator],
    label: taskQueryOperatorLabels[operator],
    arity: operatorArity(operator),
    acceptsMany: acceptsManyValues(operator),
  };

  if (!isRelativeDayOperator(operator)) return descriptor;

  // "is in the next 7 days" counts days rather than naming one, so it types its own value.
  return {
    ...descriptor,
    inputType: 'number',
    valueLabel: $localize`:Label of the day-count field in the query builder:Days`,
    valuePlaceholder: '7',
    valueSuffix: dayUnit,
  };
}

function inputTypeFor(valueType: TaskQueryValueType): QueryBuilderInputType {
  switch (valueType) {
    case TaskQueryValueType.date:
    case TaskQueryValueType.timestamp:
      return 'date';
    case TaskQueryValueType.number:
      return 'number';
    default:
      return 'text';
  }
}
