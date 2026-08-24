import {
  findQueryField,
  findQueryOperator,
  operatorValueCount,
  QueryBuilderCatalog,
  QueryBuilderCondition,
  QueryBuilderGroup,
  QueryBuilderGroupOperator,
  queryOptionLabel,
} from './query-builder.models';

const andSeparator = $localize`:Joins query conditions in an all-of group, with surrounding spaces: and `;
const orSeparator = $localize`:Joins query conditions in an any-of group, with surrounding spaces: or `;
const notPrefix = $localize`:Prefix for a query fragment that must not match:not`;
const rangeJoiner = $localize`:Joins the two ends of a range in a query summary:and`;
const valueSeparator = $localize`:Joins values in a set-membership query summary, with a trailing space:, `;

export function explainQueryGroup(
  group: QueryBuilderGroup,
  catalog: QueryBuilderCatalog
): string {
  if (isEmptyGroup(group)) return '';

  return explainGroup(group, catalog, true);
}

function explainGroup(
  group: QueryBuilderGroup,
  catalog: QueryBuilderCatalog,
  isRoot: boolean
): string {
  if (isEmptyGroup(group)) {
    return $localize`:Summary fragment standing in for a nested group with no conditions:nothing`;
  }

  const conditionParts = group.conditions.map((condition) => {
    return explainCondition(condition, catalog);
  });
  const groupParts = group.groups.map((nested) => {
    return explainGroup(nested, catalog, false);
  });
  const parts = [...conditionParts, ...groupParts];

  if (group.operator === QueryBuilderGroupOperator.none) {
    return `${notPrefix} (${parts.join(orSeparator)})`;
  }

  const isConjunction = group.operator === QueryBuilderGroupOperator.all;
  const joined = parts.join(isConjunction ? andSeparator : orSeparator);
  const needsBrackets = !isRoot && parts.length > 1;

  return needsBrackets ? `(${joined})` : joined;
}

function explainCondition(
  condition: QueryBuilderCondition,
  catalog: QueryBuilderCatalog
): string {
  const field = findQueryField(catalog, condition.field);
  const fieldName = field?.name ?? condition.field;
  const operator = findQueryOperator(field, condition.operator);
  const operatorLabel = operator?.label ?? '';
  const arity = operatorValueCount(operator);

  if (arity === 0) return `${fieldName} ${operatorLabel}`;

  // A suffixed operator counts something rather than naming it, so the raw value reads better
  // than an option label ever would: "due date is in the next 7 days".
  if (operator?.valueSuffix) {
    const amount = condition.values[0] ?? '0';

    return `${fieldName} ${operatorLabel} ${amount} ${operator.valueSuffix}`;
  }

  const labels = condition.values.map((value) => {
    return queryOptionLabel(field, value);
  });

  if (arity === 2) {
    const from = labels[0] ?? '?';
    const to = labels[1] ?? '?';

    return `${fieldName} ${operatorLabel} ${from} ${rangeJoiner} ${to}`;
  }

  if (operator?.acceptsMany) {
    const joined = labels.join(valueSeparator);

    return `${fieldName} ${operatorLabel} ${joined || '?'}`;
  }

  return `${fieldName} ${operatorLabel} ${labels[0] ?? '?'}`;
}

function isEmptyGroup(group: QueryBuilderGroup): boolean {
  return !group.conditions.length && !group.groups.length;
}
