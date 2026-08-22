import {
  acceptsManyValues,
  isRelativeDayOperator,
  operatorArity,
  taskQueryOperatorLabels,
} from '../models/task-query-copy';
import {
  TaskQueryCatalog,
  TaskQueryCondition,
  TaskQueryGroup,
  TaskQueryGroupOperator,
  TaskQueryOperator,
} from '../models/task-view.models';

export interface QueryExplanationContext {
  catalog: TaskQueryCatalog;
  labelFor: (fieldKey: string, value: string) => string;
}

const andSeparator = $localize`:Joins query conditions in an all-of group, with surrounding spaces: and `;
const orSeparator = $localize`:Joins query conditions in an any-of group, with surrounding spaces: or `;
const notPrefix = $localize`:Prefix for a query fragment that must not match:not`;
const rangeJoiner = $localize`:Joins the two ends of a range in a query summary:and`;
const dayUnit = $localize`:Unit after a relative day count in a query summary:days`;
const valueSeparator = $localize`:Joins values in a set-membership query summary, with a trailing space:, `;

export function explainQuery(
  group: TaskQueryGroup,
  context: QueryExplanationContext
): string {
  const isEmpty = !group.conditions.length && !group.groups.length;

  if (isEmpty) {
    return $localize`:Summary shown when a query has no conditions and therefore matches nothing:No conditions yet, so this view matches no tasks.`;
  }

  return explainGroup(group, context, true);
}

function explainGroup(
  group: TaskQueryGroup,
  context: QueryExplanationContext,
  isRoot: boolean
): string {
  const isEmpty = !group.conditions.length && !group.groups.length;

  if (isEmpty) {
    return $localize`:Summary fragment standing in for a nested group with no conditions:nothing`;
  }

  const conditionParts = group.conditions.map((condition) => {
    return explainCondition(condition, context);
  });
  const groupParts = group.groups.map((nested) => {
    return explainGroup(nested, context, false);
  });
  const parts = [...conditionParts, ...groupParts];

  if (group.operator === TaskQueryGroupOperator.none) {
    return `${notPrefix} (${parts.join(orSeparator)})`;
  }

  const isConjunction = group.operator === TaskQueryGroupOperator.all;
  const joined = parts.join(isConjunction ? andSeparator : orSeparator);
  const needsBrackets = !isRoot && parts.length > 1;

  return needsBrackets ? `(${joined})` : joined;
}

function explainCondition(
  condition: TaskQueryCondition,
  context: QueryExplanationContext
): string {
  const field = context.catalog.fields.find(
    (candidate) => candidate.key === condition.field
  );
  const fieldName = field?.name ?? condition.field;
  const operatorLabel = taskQueryOperatorLabels[condition.operator] ?? '';

  if (operatorArity(condition.operator) === 0) {
    return `${fieldName} ${operatorLabel}`;
  }

  if (isRelativeDayOperator(condition.operator)) {
    const days = condition.values[0] ?? '0';

    return `${fieldName} ${operatorLabel} ${days} ${dayUnit}`;
  }

  const labels = condition.values.map((value) => {
    return context.labelFor(condition.field, value);
  });

  if (condition.operator === TaskQueryOperator.between) {
    const from = labels[0] ?? '?';
    const to = labels[1] ?? '?';

    return `${fieldName} ${operatorLabel} ${from} ${rangeJoiner} ${to}`;
  }

  if (acceptsManyValues(condition.operator)) {
    const joined = labels.join(valueSeparator);

    return `${fieldName} ${operatorLabel} ${joined || '?'}`;
  }

  return `${fieldName} ${operatorLabel} ${labels[0] ?? '?'}`;
}
