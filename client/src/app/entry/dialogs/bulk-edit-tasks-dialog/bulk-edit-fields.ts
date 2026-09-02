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

export type BulkEditFieldType =
  'select' | 'enum' | 'date' | 'number' | 'tags' | 'people';

export interface BulkEditFieldDefinition {
  key: BulkEditFieldKey;
  label: string;
  type: BulkEditFieldType;
}

export const bulkEditFieldTypeLabels: Record<BulkEditFieldType, string> = {
  select: $localize`:Kind of control a bulk-edit field brings, shown beside its name:select`,
  enum: $localize`:Kind of control a bulk-edit field brings, shown beside its name:enum`,
  date: $localize`:Kind of control a bulk-edit field brings, shown beside its name:date`,
  number: $localize`:Kind of control a bulk-edit field brings, shown beside its name:number`,
  tags: $localize`:Kind of control a bulk-edit field brings, shown beside its name:tags`,
  people: $localize`:Kind of control a bulk-edit field brings, shown beside its name:people`,
};

// The order rows appear in, and the order the add-a-field menu offers what is left.
export const bulkEditFields: readonly BulkEditFieldDefinition[] = [
  {
    key: 'status',
    label: $localize`:Label of the status field:Status`,
    type: 'select',
  },
  {
    key: 'priority',
    label: $localize`:Label of the priority field:Priority`,
    type: 'enum',
  },
  {
    key: 'dueDate',
    label: $localize`:Label of the due date field:Due date`,
    type: 'date',
  },
  {
    key: 'estimateType',
    label: $localize`:Label of the estimate-unit field:Estimate type`,
    type: 'enum',
  },
  {
    key: 'estimateValue',
    label: $localize`:Label of the story points field:Story points`,
    type: 'number',
  },
  {
    key: 'tags',
    label: $localize`:Label of the tags field:Tags`,
    type: 'tags',
  },
  {
    key: 'assignees',
    label: $localize`:Label of the assignees field:Assignees`,
    type: 'people',
  },
  {
    key: 'project',
    label: $localize`:Label of the project field:Project`,
    type: 'select',
  },
  {
    key: 'sprint',
    label: $localize`:Label of the sprint field:Sprint`,
    type: 'select',
  },
];
