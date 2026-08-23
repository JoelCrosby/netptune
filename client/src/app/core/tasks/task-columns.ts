import { TaskPriority } from '@core/enums/task-priority';
import { SprintStatus } from '@core/enums/sprint-status';
import { StatusCategory } from '@core/models/status';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import {
  DatatableColumn,
  DatatableColumnPreference,
} from '@static/components/datatable/datatable.types';
import { TaskAssigneesComponent } from '@static/components/task-assignees.component';
import { TaskDateComponent } from '@static/components/task-date.component';
import { TaskNameComponent } from '@static/components/task-name.component';
import { TaskPriorityComponent } from '@static/components/task-priority.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { TaskSprintComponent } from '@static/components/task-sprint.component';
import { TaskStatusPillComponent } from '@static/components/task-status-pill.component';

// The shape every task-shaped row satisfies. TaskViewModel, ScheduledTask and
// RoadmapTask all widen to this, so one catalog serves every task table.
export interface TaskColumnRow {
  id: number;
  systemId: string;
  name: string;
  projectName?: string;
  statusName?: string;
  statusColor?: string | null;
  statusCategory?: StatusCategory;
  priority?: TaskPriority | null;
  assignees?: readonly AssigneeViewModel[];
  sprintName?: string | null;
  sprintStatus?: SprintStatus | null;
  startDate?: string | Date | null;
  dueDate?: string | Date | null;
  updatedAt?: string | Date;
  hasComments?: boolean;
}

export type TaskColumnId =
  | 'systemId'
  | 'name'
  | 'project'
  | 'sprint'
  | 'status'
  | 'priority'
  | 'assignees'
  | 'dueDate'
  | 'startDate'
  | 'updatedAt';

// sortKey is the field name the tasks API sorts on. Endpoints that name their
// sort fields differently (roadmap and calendar use statusName rather than
// status) pass an override through TaskColumnOptions.
const catalog: Record<TaskColumnId, DatatableColumn<TaskColumnRow>> = {
  systemId: {
    id: 'systemId',
    header: $localize`:Column heading for the task reference:Key`,
    accessor: 'systemId',
    sortable: true,
    sortKey: 'systemId',
    widthClass: 'w-28',
    cell: {
      component: TaskScopeIdComponent,
      inputs: (task) => ({ id: task.systemId }),
    },
  },
  name: {
    id: 'name',
    header: $localize`:Column heading for the task name:Task`,
    accessor: 'name',
    sortable: true,
    sortKey: 'name',
    cellClass: 'min-w-64',
    cell: {
      component: TaskNameComponent,
      inputs: (task) => ({ name: task.name }),
    },
  },
  project: {
    id: 'project',
    header: $localize`:Column heading for the task project:Project`,
    accessor: 'projectName',
    sortable: true,
    sortKey: 'projectName',
    widthClass: 'w-44',
    cellClass: 'text-muted truncate text-sm',
  },
  sprint: {
    id: 'sprint',
    header: $localize`:Column heading for the task sprint:Sprint`,
    accessor: 'sprintName',
    sortable: true,
    sortKey: 'sprint',
    widthClass: 'w-40',
    cell: {
      component: TaskSprintComponent,
      inputs: (task) => ({ name: task.sprintName, status: task.sprintStatus }),
    },
  },
  status: {
    id: 'status',
    header: $localize`:Column heading for the task status:Status`,
    accessor: 'statusName',
    sortable: true,
    sortKey: 'status',
    widthClass: 'w-40',
    cell: {
      component: TaskStatusPillComponent,
      inputs: (task) => ({
        name: task.statusName,
        color: task.statusColor,
        category: task.statusCategory,
      }),
    },
  },
  priority: {
    id: 'priority',
    header: $localize`:Column heading for the task priority:Priority`,
    accessor: 'priority',
    sortable: true,
    sortKey: 'priority',
    widthClass: 'w-32',
    cell: {
      component: TaskPriorityComponent,
      inputs: (task) => ({ priority: task.priority }),
    },
  },
  assignees: {
    id: 'assignees',
    header: $localize`:Column heading for the task assignees:Assignees`,
    sortable: true,
    sortKey: 'assignees',
    widthClass: 'w-40',
    cell: {
      component: TaskAssigneesComponent,
      inputs: (task) => ({ assignees: task.assignees ?? [] }),
    },
  },
  dueDate: {
    id: 'dueDate',
    header: $localize`:Column heading for the task due date:Due`,
    accessor: 'dueDate',
    sortable: true,
    sortKey: 'dueDate',
    widthClass: 'w-32',
    cell: {
      component: TaskDateComponent,
      inputs: (task) => ({ value: task.dueDate }),
    },
  },
  startDate: {
    id: 'startDate',
    header: $localize`:Column heading for the task start date:Start`,
    accessor: 'startDate',
    sortable: true,
    sortKey: 'startDate',
    widthClass: 'w-32',
    cell: {
      component: TaskDateComponent,
      inputs: (task) => ({ value: task.startDate }),
    },
  },
  updatedAt: {
    id: 'updatedAt',
    header: $localize`:Column heading for when the task last changed:Updated`,
    accessor: 'updatedAt',
    sortable: true,
    sortKey: 'updatedAt',
    widthClass: 'w-40',
    cell: {
      component: TaskDateComponent,
      inputs: (task) => ({ value: task.updatedAt }),
    },
  },
};

const catalogOrder = Object.keys(catalog) as TaskColumnId[];

const defaultColumnIds: TaskColumnId[] = [
  'systemId',
  'name',
  'status',
  'assignees',
  'dueDate',
];

export interface TaskNameCellOptions<T extends TaskColumnRow> {
  link?: (task: T) => unknown[];
  action?: (task: T) => void;
  showComments?: boolean;
  flagNames?: (task: T) => readonly string[];
}

// The name column is the one cell every surface renders differently, because
// activating it means something different on each. This keeps the markup shared
// and lets the caller supply only the behaviour.
export function taskNameCell<T extends TaskColumnRow>(
  options: TaskNameCellOptions<T> = {}
): Partial<DatatableColumn<T>> {
  const { link, action, showComments, flagNames } = options;

  return {
    cell: {
      component: TaskNameComponent,
      inputs: (task) => ({
        name: task.name,
        link: link ? link(task) : null,
        action: action ? () => action(task) : null,
        hasComments: showComments ? (task.hasComments ?? false) : false,
        flagNames: flagNames ? flagNames(task) : [],
      }),
    },
  };
}

export interface TaskColumnOptions<T extends TaskColumnRow> {
  overrides?: Partial<Record<TaskColumnId, Partial<DatatableColumn<T>>>>;
}

// The catalog is written against TaskColumnRow, which every task row widens to.
// The cast keeps that single definition usable from tables typed to a concrete
// row model rather than duplicating the catalog per model.
function typedColumn<T extends TaskColumnRow>(
  id: TaskColumnId
): DatatableColumn<T> {
  return catalog[id] as unknown as DatatableColumn<T>;
}

export function taskColumns<T extends TaskColumnRow>(
  ids: readonly TaskColumnId[],
  options: TaskColumnOptions<T> = {}
): DatatableColumn<T>[] {
  return ids.map((id) => {
    const override = options.overrides?.[id];

    if (!override) return typedColumn<T>(id);

    return { ...typedColumn<T>(id), ...override };
  });
}

export function allTaskColumns<T extends TaskColumnRow>(
  options: TaskColumnOptions<T> = {}
): DatatableColumn<T>[] {
  return taskColumns<T>(catalogOrder, options);
}

export function defaultTaskColumnPreferences(): DatatableColumnPreference[] {
  return catalogOrder.map((id) => ({
    id,
    visible: defaultColumnIds.includes(id),
  }));
}

// Saved preferences decide order as well as visibility, so columns come back in
// the order they were stored and anything added to the catalog since is
// appended rather than dropped.
export function taskColumnsFromPreferences<T extends TaskColumnRow>(
  preferences: readonly DatatableColumnPreference[],
  options: TaskColumnOptions<T> = {}
): DatatableColumn<T>[] {
  if (!preferences.length) {
    return taskColumns<T>(defaultColumnIds, options);
  }

  const known = new Set<string>(catalogOrder);
  const ordered = preferences
    .map((preference) => preference.id)
    .filter((id): id is TaskColumnId => known.has(id));
  const seen = new Set<string>(ordered);
  const added = catalogOrder.filter((id) => !seen.has(id));

  return taskColumns<T>([...ordered, ...added], options);
}

// Only the columns a saved view chose to show, in the order it stored them.
export function visibleTaskColumns<T extends TaskColumnRow>(
  preferences: readonly DatatableColumnPreference[],
  options: TaskColumnOptions<T> = {}
): DatatableColumn<T>[] {
  const visible = new Set(visibleTaskColumnIds(preferences));

  return taskColumnsFromPreferences<T>(preferences, options).filter(
    (column) => {
      return visible.has(column.id);
    }
  );
}

export function visibleTaskColumnIds(
  preferences: readonly DatatableColumnPreference[]
): string[] {
  if (!preferences.length) return [...defaultColumnIds];

  return preferences
    .filter((preference) => preference.visible)
    .map((preference) => preference.id);
}
