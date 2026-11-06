import { AddFieldMenuOption } from '@static/components/add-field-menu/add-field-menu.component';
import {
  AutomationAction,
  AutomationDateUpdate,
  AutomationDateUpdateMode,
} from '@core/automations/automation.models';

export type TaskUpdateFieldKey =
  | 'status'
  | 'priority'
  | 'name'
  | 'description'
  | 'owner'
  | 'assignees'
  | 'addTags'
  | 'removeTags'
  | 'startDate'
  | 'dueDate'
  | 'estimate'
  | 'sprint'
  | 'boardGroup';

export type TaskUpdateFieldDefinition = AddFieldMenuOption<TaskUpdateFieldKey>;

// Rules loaded from the server have no such list, so it is worked out from the values they carry.
export interface TaskUpdateDraft extends AutomationAction {
  taskUpdateFields?: TaskUpdateFieldKey[];
}

// The order rows appear in, and the order the add-a-field menu offers what is left.
export const taskUpdateFields: readonly TaskUpdateFieldDefinition[] = [
  {
    key: 'status',
    label: $localize`:Label of the status field:Status`,
    kind: 'select',
  },
  {
    key: 'priority',
    label: $localize`:Label of the priority field:Priority`,
    kind: 'enum',
  },
  {
    key: 'name',
    label: $localize`:Label of the task name field in an update task action:Name`,
    kind: 'text',
  },
  {
    key: 'description',
    label: $localize`:Label of the description field:Description`,
    kind: 'text',
  },
  {
    key: 'owner',
    label: $localize`:Label of the task owner field:Owner`,
    kind: 'people',
  },
  {
    key: 'assignees',
    label: $localize`:Label of the assignees field:Assignees`,
    kind: 'people',
  },
  {
    key: 'addTags',
    label: $localize`:Label of the add tags field:Add tags`,
    kind: 'tags',
  },
  {
    key: 'removeTags',
    label: $localize`:Label of the remove tags field:Remove tags`,
    kind: 'tags',
  },
  {
    key: 'startDate',
    label: $localize`:Label of the task start date field:Start date`,
    kind: 'date',
  },
  {
    key: 'dueDate',
    label: $localize`:Label of the task due date field:Due date`,
    kind: 'date',
  },
  {
    key: 'estimate',
    label: $localize`:Label of the task estimate field:Estimate`,
    kind: 'number',
  },
  {
    key: 'sprint',
    label: $localize`:Label of the sprint field:Sprint`,
    kind: 'select',
  },
  {
    key: 'boardGroup',
    label: $localize`:Label of the board group field:Board group`,
    kind: 'select',
  },
];

export function addedTaskUpdateFields(
  action: TaskUpdateDraft
): TaskUpdateFieldKey[] {
  if (action.taskUpdateFields) return action.taskUpdateFields;

  return taskUpdateFields
    .map((field) => field.key)
    .filter((key) => isSavedTaskUpdateField(action, key));
}

export function incompleteTaskUpdateField(
  action: TaskUpdateDraft
): TaskUpdateFieldDefinition | null {
  const added = addedTaskUpdateFields(action);
  const incomplete = taskUpdateFields.find((field) => {
    return added.includes(field.key) && isIncomplete(action, field.key);
  });

  return incomplete ?? null;
}

function isSavedTaskUpdateField(
  action: AutomationAction,
  key: TaskUpdateFieldKey
): boolean {
  switch (key) {
    case 'status':
      return hasValue(action.statusId);
    case 'priority':
      return hasValue(action.priority);
    case 'name':
      return hasValue(action.taskName);
    case 'description':
      return hasValue(action.taskDescription) || !!action.clearDescription;
    case 'owner':
      return hasValue(action.ownerId) || !!action.clearOwner;
    case 'assignees':
      return hasValue(action.assigneeIds);
    case 'addTags':
      return !!action.addTags?.length;
    case 'removeTags':
      return !!action.removeTags?.length;
    case 'startDate':
      return hasValue(action.startDate);
    case 'dueDate':
      return hasValue(action.dueDate);
    case 'estimate':
      return (
        hasValue(action.estimateType) ||
        hasValue(action.estimateValue) ||
        !!action.clearEstimate
      );
    case 'sprint':
      return hasValue(action.sprintId) || !!action.clearSprint;
    case 'boardGroup':
      return hasValue(action.boardGroupId);
  }
}

function isIncomplete(
  action: AutomationAction,
  key: TaskUpdateFieldKey
): boolean {
  switch (key) {
    case 'name':
      return !action.taskName?.trim();
    case 'description':
      return !action.clearDescription && !action.taskDescription?.trim();
    case 'assignees':
      return !!action.appendAssignees && !action.assigneeIds?.length;
    case 'estimate':
      return !action.clearEstimate && !hasValue(action.estimateValue);
    case 'startDate':
      return isIncompleteDate(action.startDate);
    case 'dueDate':
      return isIncompleteDate(action.dueDate);
    default:
      return !isSavedTaskUpdateField(action, key);
  }
}

function isIncompleteDate(
  update: AutomationDateUpdate | null | undefined
): boolean {
  if (!update) return true;

  switch (update.mode) {
    case AutomationDateUpdateMode.absolute:
      return !update.date;
    case AutomationDateUpdateMode.relativeDays:
    case AutomationDateUpdateMode.relativeBusinessDays:
      return !Number.isInteger(update.offset);
    case AutomationDateUpdateMode.clear:
      return false;
  }
}

function hasValue<T>(value: T | null | undefined): value is T {
  return value !== null && value !== undefined;
}
