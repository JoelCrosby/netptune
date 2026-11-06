import { AddFieldMenuOption } from '@static/components/add-field-menu/add-field-menu.component';

export type BulkEditFieldKey =
  | 'status'
  | 'priority'
  | 'dueDate'
  | 'estimateType'
  | 'estimateValue'
  | 'tags'
  | 'assignees'
  | 'project'
  | 'sprint';

export type BulkEditFieldDefinition = AddFieldMenuOption<BulkEditFieldKey>;

// The order rows appear in, and the order the add-a-field menu offers what is left.
export const bulkEditFields: readonly BulkEditFieldDefinition[] = [
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
    key: 'dueDate',
    label: $localize`:Label of the due date field:Due date`,
    kind: 'date',
  },
  {
    key: 'estimateType',
    label: $localize`:Label of the estimate-unit field:Estimate type`,
    kind: 'enum',
  },
  {
    key: 'estimateValue',
    label: $localize`:Label of the story points field:Story points`,
    kind: 'number',
  },
  {
    key: 'tags',
    label: $localize`:Label of the tags field:Tags`,
    kind: 'tags',
  },
  {
    key: 'assignees',
    label: $localize`:Label of the assignees field:Assignees`,
    kind: 'people',
  },
  {
    key: 'project',
    label: $localize`:Label of the project field:Project`,
    kind: 'select',
  },
  {
    key: 'sprint',
    label: $localize`:Label of the sprint field:Sprint`,
    kind: 'select',
  },
];
