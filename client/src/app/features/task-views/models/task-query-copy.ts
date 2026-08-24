import { TaskQueryOperator } from './task-view.models';

export const taskQueryOperatorLabels: Record<TaskQueryOperator, string> = {
  [TaskQueryOperator.equals]: $localize`:Query operator matching an exact value:is`,
  [TaskQueryOperator.notEquals]: $localize`:Query operator excluding an exact value:is not`,
  [TaskQueryOperator.in]: $localize`:Query operator matching any of several values:is any of`,
  [TaskQueryOperator.notIn]: $localize`:Query operator excluding several values:is none of`,
  [TaskQueryOperator.contains]: $localize`:Query operator matching text anywhere in a field:contains`,
  [TaskQueryOperator.notContains]: $localize`:Query operator excluding text anywhere in a field:does not contain`,
  [TaskQueryOperator.startsWith]: $localize`:Query operator matching the start of a field:starts with`,
  [TaskQueryOperator.isEmpty]: $localize`:Query operator matching an unset field:is empty`,
  [TaskQueryOperator.isNotEmpty]: $localize`:Query operator matching a set field:is not empty`,
  [TaskQueryOperator.greaterThan]: $localize`:Query operator matching values above a number or date:is after`,
  [TaskQueryOperator.greaterThanOrEqual]: $localize`:Query operator matching values at or above a number or date:is on or after`,
  [TaskQueryOperator.lessThan]: $localize`:Query operator matching values below a number or date:is before`,
  [TaskQueryOperator.lessThanOrEqual]: $localize`:Query operator matching values at or below a number or date:is on or before`,
  [TaskQueryOperator.between]: $localize`:Query operator matching a range:is between`,
  [TaskQueryOperator.inNextDays]: $localize`:Query operator matching a date within the coming days:is in the next`,
  [TaskQueryOperator.inLastDays]: $localize`:Query operator matching a date within the past days:is in the last`,
  [TaskQueryOperator.isOverdue]: $localize`:Query operator matching tasks past their due date:is overdue`,
};

export const emptyTaskQueryMessage = $localize`:Summary shown when a query has no conditions and therefore matches nothing:No conditions yet, so this view matches no tasks.`;

export function operatorArity(operator: TaskQueryOperator): number {
  switch (operator) {
    case TaskQueryOperator.isEmpty:
    case TaskQueryOperator.isNotEmpty:
    case TaskQueryOperator.isOverdue:
      return 0;
    case TaskQueryOperator.between:
      return 2;
    default:
      return 1;
  }
}

export function acceptsManyValues(operator: TaskQueryOperator): boolean {
  return (
    operator === TaskQueryOperator.in || operator === TaskQueryOperator.notIn
  );
}

export function isRelativeDayOperator(operator: TaskQueryOperator): boolean {
  return (
    operator === TaskQueryOperator.inNextDays ||
    operator === TaskQueryOperator.inLastDays
  );
}
