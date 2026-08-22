import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import {
  DatatableColumn,
  DatatableColumnPreference,
} from '@static/components/datatable/datatable.types';

const availableColumns: DatatableColumn<TaskViewModel>[] = [
  {
    id: 'systemId',
    header: $localize`:Column heading for the task reference:Id`,
    accessor: 'systemId',
    sortable: true,
    sortKey: 'systemId',
    widthClass: 'w-28',
  },
  {
    id: 'name',
    header: $localize`:Column heading for the task name:Name`,
    accessor: 'name',
    sortable: true,
    sortKey: 'name',
  },
  {
    id: 'status',
    header: $localize`:Column heading for the task status:Status`,
    accessor: 'statusName',
    sortable: true,
    sortKey: 'status',
    widthClass: 'w-40',
  },
  {
    id: 'assignees',
    header: $localize`:Column heading for the task assignees:Assignees`,
    sortable: true,
    sortKey: 'assignees',
    widthClass: 'w-44',
  },
  {
    id: 'priority',
    header: $localize`:Column heading for the task priority:Priority`,
    sortable: true,
    sortKey: 'priority',
    widthClass: 'w-28',
  },
  {
    id: 'project',
    header: $localize`:Column heading for the task project:Project`,
    accessor: 'projectName',
    sortable: true,
    sortKey: 'projectName',
    widthClass: 'w-40',
  },
  {
    id: 'sprint',
    header: $localize`:Column heading for the task sprint:Sprint`,
    accessor: 'sprintName',
    sortable: true,
    sortKey: 'sprint',
    widthClass: 'w-40',
  },
  {
    id: 'dueDate',
    header: $localize`:Column heading for the task due date:Due`,
    accessor: 'dueDate',
    sortable: true,
    sortKey: 'dueDate',
    widthClass: 'w-32',
  },
  {
    id: 'startDate',
    header: $localize`:Column heading for the task start date:Start`,
    accessor: 'startDate',
    sortable: true,
    sortKey: 'startDate',
    widthClass: 'w-32',
  },
  {
    id: 'updatedAt',
    header: $localize`:Column heading for when the task last changed:Updated`,
    accessor: 'updatedAt',
    sortable: true,
    sortKey: 'updatedAt',
    widthClass: 'w-36',
  },
];

const defaultColumnIds = ['systemId', 'name', 'status', 'assignees', 'dueDate'];

export function allTaskViewColumns(): DatatableColumn<TaskViewModel>[] {
  return availableColumns;
}

export function defaultColumnPreferences(): DatatableColumnPreference[] {
  return availableColumns.map((column) => ({
    id: column.id,
    visible: defaultColumnIds.includes(column.id),
  }));
}

// The saved preferences decide order as well as visibility, so columns are emitted in the order the
// view stored them and anything added to the catalog since is appended rather than dropped.
export function taskViewColumns(
  preferences: readonly DatatableColumnPreference[]
): DatatableColumn<TaskViewModel>[] {
  if (!preferences.length) {
    return availableColumns.filter((column) => {
      return defaultColumnIds.includes(column.id);
    });
  }

  const byId = new Map(availableColumns.map((column) => [column.id, column]));
  const ordered = preferences
    .map((preference) => byId.get(preference.id))
    .filter(
      (column): column is DatatableColumn<TaskViewModel> => column != null
    );
  const seen = new Set(ordered.map((column) => column.id));
  const added = availableColumns.filter((column) => !seen.has(column.id));

  return [...ordered, ...added];
}

export function visibleColumnIds(
  preferences: readonly DatatableColumnPreference[]
): string[] {
  if (!preferences.length) return defaultColumnIds;

  return preferences
    .filter((preference) => preference.visible)
    .map((preference) => preference.id);
}
