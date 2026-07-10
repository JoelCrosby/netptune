import { BoardViewTask } from '@core/models/view-models/board-view';

export type BoardTaskSortField =
  | 'custom'
  | 'status'
  | 'priority'
  | 'createdAt'
  | 'updatedAt';

export type BoardTaskSortDirection = 'asc' | 'desc';

export interface BoardTaskSort {
  field: BoardTaskSortField;
  direction: BoardTaskSortDirection;
}

export type BoardTaskSortMap = Record<string, BoardTaskSort>;

export const DEFAULT_BOARD_TASK_SORT: BoardTaskSort = {
  field: 'custom',
  direction: 'asc',
};

const SORT_FIELDS: readonly BoardTaskSortField[] = [
  'custom',
  'status',
  'priority',
  'createdAt',
  'updatedAt',
];

export const boardTaskSortFieldLabels: Record<BoardTaskSortField, string> = {
  custom: 'Manual',
  status: 'Status',
  priority: 'Priority',
  createdAt: 'Created',
  updatedAt: 'Updated',
};

export const boardTaskSortFieldOptions = SORT_FIELDS.map((value) => ({
  value,
  label: boardTaskSortFieldLabels[value],
}));

const isSortField = (value: string): value is BoardTaskSortField =>
  (SORT_FIELDS as readonly string[]).includes(value);

export function encodeBoardTaskSort(sort: BoardTaskSort): string {
  return `${sort.field}:${sort.direction}`;
}

export function decodeBoardTaskSort(value: unknown): BoardTaskSort {
  if (typeof value !== 'string') return DEFAULT_BOARD_TASK_SORT;

  const [field, direction] = value.split(':');

  if (!isSortField(field)) return DEFAULT_BOARD_TASK_SORT;

  return {
    field,
    direction: direction === 'desc' ? 'desc' : 'asc',
  };
}

export function parseBoardTaskSortMap(value: unknown): BoardTaskSortMap {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return {};

  const result: BoardTaskSortMap = {};

  for (const [boardId, encoded] of Object.entries(value)) {
    if (typeof encoded !== 'string') continue;

    result[boardId] = decodeBoardTaskSort(encoded);
  }

  return result;
}

export function boardTaskSortForBoard(
  value: unknown,
  boardId: number
): BoardTaskSort {
  return parseBoardTaskSortMap(value)[boardId] ?? DEFAULT_BOARD_TASK_SORT;
}

const isDefaultSort = (sort: BoardTaskSort): boolean =>
  sort.field === DEFAULT_BOARD_TASK_SORT.field &&
  sort.direction === DEFAULT_BOARD_TASK_SORT.direction;

export function withBoardTaskSort(
  value: unknown,
  boardId: number,
  sort: BoardTaskSort
): Record<string, string> {
  const next = parseBoardTaskSortMap(value);

  if (isDefaultSort(sort)) {
    // eslint-disable-next-line @typescript-eslint/no-dynamic-delete
    delete next[boardId];
  } else {
    next[boardId] = sort;
  }

  return Object.fromEntries(
    Object.entries(next).map(([id, value]) => [id, encodeBoardTaskSort(value)])
  );
}

const timestamp = (value: string): number => {
  const time = Date.parse(value);
  return Number.isNaN(time) ? 0 : time;
};

function compareByField(
  a: BoardViewTask,
  b: BoardViewTask,
  field: BoardTaskSortField
): number {
  switch (field) {
    case 'status':
      return (
        a.statusCategory - b.statusCategory ||
        a.statusName.localeCompare(b.statusName)
      );
    case 'priority':
      return (a.priority ?? 0) - (b.priority ?? 0);
    case 'createdAt':
      return timestamp(a.createdAt) - timestamp(b.createdAt);
    case 'updatedAt':
      return timestamp(a.updatedAt) - timestamp(b.updatedAt);
    case 'custom':
    default:
      return a.sortOrder - b.sortOrder;
  }
}

export function sortBoardViewTasks<T extends BoardViewTask>(
  tasks: T[],
  sort: BoardTaskSort
): T[] {
  if (sort.field === 'custom') {
    return [...tasks].sort((a, b) => a.sortOrder - b.sortOrder);
  }

  const factor = sort.direction === 'desc' ? -1 : 1;

  return [...tasks].sort((a, b) => {
    const result = compareByField(a, b, sort.field);
    return result !== 0 ? result * factor : a.sortOrder - b.sortOrder;
  });
}
