import { LucideIconInput } from '@lucide/angular';

/**
 * Editing vocabulary for the query builder. Saved views and automation rules project their own
 * field and operator enums onto these types, so a catalog describes every operator it offers and
 * the builder never knows one by name. Operator-level overrides win over the field's own.
 */
export enum QueryBuilderGroupOperator {
  all = 0,
  any = 1,
  none = 2,
}

export type QueryBuilderInputType = 'text' | 'number' | 'date';

export interface QueryBuilderOption {
  value: string;
  label: string;
}

export interface QueryBuilderOperator {
  key: string;
  label: string;
  // Value slots the operator fills: none for "is empty", one for "is", two for a range.
  arity: number;
  acceptsMany?: boolean;
  icon?: LucideIconInput;
  inputType?: QueryBuilderInputType;
  valueLabel?: string;
  valuePlaceholder?: string;
  // Trailing unit in the summary, e.g. "days" in "is in the next 7 days".
  valueSuffix?: string;
}

export interface QueryBuilderField {
  key: string;
  name: string;
  operators: QueryBuilderOperator[];
  inputType?: QueryBuilderInputType;
  // When present the value is picked from this list instead of typed.
  options?: QueryBuilderOption[];
  valuePlaceholder?: string;
}

export interface QueryBuilderCatalog {
  fields: QueryBuilderField[];
  maximumDepth: number;
  maximumConditionCount?: number;
}

export interface QueryBuilderCondition {
  field: string;
  operator: string;
  values: string[];
}

export interface QueryBuilderGroup {
  operator: QueryBuilderGroupOperator;
  conditions: QueryBuilderCondition[];
  groups: QueryBuilderGroup[];
}

export interface QueryBuilderError {
  path: string;
  message: string;
}

export const queryBuilderGroupOperatorLabels: Record<
  QueryBuilderGroupOperator,
  string
> = {
  [QueryBuilderGroupOperator.all]: $localize`:Group operator requiring every condition to match:All`,
  [QueryBuilderGroupOperator.any]: $localize`:Group operator requiring at least one condition to match:Any`,
  [QueryBuilderGroupOperator.none]: $localize`:Group operator requiring no condition to match:None`,
};

export const queryBuilderGroupOperatorCodes: Record<
  QueryBuilderGroupOperator,
  string
> = {
  [QueryBuilderGroupOperator.all]: 'AND',
  [QueryBuilderGroupOperator.any]: 'OR',
  [QueryBuilderGroupOperator.none]: 'NOT',
};

export function findQueryField(
  catalog: QueryBuilderCatalog,
  fieldKey: string
): QueryBuilderField | undefined {
  return catalog.fields.find((candidate) => candidate.key === fieldKey);
}

export function findQueryOperator(
  field: QueryBuilderField | undefined,
  operatorKey: string
): QueryBuilderOperator | undefined {
  return field?.operators.find((candidate) => candidate.key === operatorKey);
}

// A condition can outlive the operator it was written with, so an unknown one keeps its value.
export function operatorValueCount(
  operator: QueryBuilderOperator | undefined
): number {
  return operator?.arity ?? 1;
}

export function queryOptionLabel(
  field: QueryBuilderField | undefined,
  value: string
): string {
  const match = field?.options?.find((option) => option.value === value);

  return match?.label ?? value;
}

export function newQueryCondition(
  field: QueryBuilderField
): QueryBuilderCondition {
  return {
    field: field.key,
    operator: field.operators[0]?.key ?? '',
    values: [],
  };
}

export function emptyQueryBuilderGroup(
  operator = QueryBuilderGroupOperator.all
): QueryBuilderGroup {
  return { operator, conditions: [], groups: [] };
}
