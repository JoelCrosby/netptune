import { DatatableColumnPreference } from '@static/components/datatable/datatable.types';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';

export enum TaskQueryGroupOperator {
  all = 0,
  any = 1,
  none = 2,
}

export enum TaskQueryOperator {
  equals = 0,
  notEquals = 1,
  in = 2,
  notIn = 3,
  contains = 4,
  notContains = 5,
  startsWith = 6,
  isEmpty = 7,
  isNotEmpty = 8,
  greaterThan = 9,
  greaterThanOrEqual = 10,
  lessThan = 11,
  lessThanOrEqual = 12,
  between = 13,
  inNextDays = 14,
  inLastDays = 15,
  isOverdue = 16,
}

export enum TaskQueryValueType {
  text = 0,
  enum = 1,
  number = 2,
  date = 3,
  timestamp = 4,
  collection = 5,
}

export type TaskQueryOptionSource =
  | 'statuses'
  | 'status-categories'
  | 'priorities'
  | 'estimate-types'
  | 'projects'
  | 'sprints'
  | 'members'
  | 'tags'
  | 'relation-types';

export interface TaskQueryCondition {
  field: string;
  operator: TaskQueryOperator;
  values: string[];
}

export interface TaskQueryGroup {
  operator: TaskQueryGroupOperator;
  conditions: TaskQueryCondition[];
  groups: TaskQueryGroup[];
}

export interface TaskQueryField {
  key: string;
  name: string;
  valueType: TaskQueryValueType;
  operators: TaskQueryOperator[];
  optionSource?: TaskQueryOptionSource | null;
  isMultiValued: boolean;
  isSortable: boolean;
  sortKey?: string | null;
}

export interface TaskQueryCatalog {
  fields: TaskQueryField[];
  maximumDepth: number;
  maximumConditionCount: number;
}

export interface TaskViewDisplay {
  columns: DatatableColumnPreference[];
  sortBy?: string | null;
  sortDirection?: string | null;
  pageSize: number;
}

export interface TaskViewDefinition {
  version: number;
  query: TaskQueryGroup;
  display: TaskViewDisplay;
}

export interface TaskView {
  id: number;
  name: string;
  description?: string | null;
  slug: string;
  icon?: string | null;
  isShared: boolean;
  definition: TaskViewDefinition | null;
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
  isOwn: boolean;
  canEdit: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface TaskQueryValidationError {
  path: string;
  message: string;
  field?: string | null;
}

export interface TaskViewResult {
  items: TaskViewModel[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  errors: TaskQueryValidationError[];
}

export interface SaveTaskViewRequest {
  id?: number | null;
  name: string;
  description?: string | null;
  icon?: string | null;
  isShared: boolean;
  definition: TaskViewDefinition;
}

export const DEFAULT_VIEW_PAGE_SIZE = 25;

export function emptyQueryGroup(): TaskQueryGroup {
  return {
    operator: TaskQueryGroupOperator.all,
    conditions: [],
    groups: [],
  };
}

export function emptyViewDefinition(): TaskViewDefinition {
  return {
    version: 1,
    query: emptyQueryGroup(),
    display: { columns: [], pageSize: DEFAULT_VIEW_PAGE_SIZE },
  };
}
