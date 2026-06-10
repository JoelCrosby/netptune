import { ParamMap, Params } from '@angular/router';
import {
  TaskStatus,
  taskStatusLabels,
  taskStatusOptions,
} from '@core/enums/project-task-status';

export interface TaskFilterRouteParams {
  term?: string | null;
  tags?: string[];
  users?: string[];
  sprintId?: number;
  statuses?: TaskStatus[];
}

export interface TaskFilterRouteQueryParams extends Params {
  term?: string;
  tags?: string[];
  users?: string[];
  sprintId?: number;
  statuses?: string[];
}

export interface BuildTaskFilterRouteParamsOptions {
  includeStatuses?: boolean;
}

const statusByLabel = new Map(
  taskStatusOptions.map((status) => [
    normalizeStatusLabel(taskStatusLabels[status]),
    status,
  ])
);

export function parseTaskFilterRouteParams(
  paramMap: ParamMap
): TaskFilterRouteParams {
  return {
    term: normalizeTerm(paramMap.get('term')),
    tags: uniqueNonEmptyValues(paramMap.getAll('tags')),
    users: uniqueNonEmptyValues(paramMap.getAll('users')),
    sprintId: getSprintId(paramMap.get('sprintId')),
    statuses: parseStatuses(paramMap.getAll('statuses')),
  };
}

export function buildTaskFilterRouteParams(
  params: TaskFilterRouteParams,
  options: BuildTaskFilterRouteParamsOptions = {}
): TaskFilterRouteQueryParams {
  const queryParams: TaskFilterRouteQueryParams = {};
  const term = normalizeTerm(params.term ?? null);
  const tags = uniqueNonEmptyValues(params.tags ?? []);
  const users = uniqueNonEmptyValues(params.users ?? []);
  const statuses = uniqueStatuses(params.statuses ?? []);

  if (term) {
    queryParams['term'] = term;
  }

  if (tags.length) {
    queryParams['tags'] = tags;
  }

  if (users.length) {
    queryParams['users'] = users;
  }

  if (params.sprintId !== undefined) {
    queryParams['sprintId'] = params.sprintId;
  }

  if (options.includeStatuses && statuses.length) {
    queryParams['statuses'] = statuses.map((status) => taskStatusLabels[status]);
  }

  return queryParams;
}

function parseStatuses(values: string[]): TaskStatus[] {
  const statuses = values
    .map((value) => statusByLabel.get(normalizeStatusLabel(value)))
    .filter((status): status is TaskStatus => status !== undefined);

  return uniqueStatuses(statuses);
}

function uniqueStatuses(statuses: TaskStatus[]): TaskStatus[] {
  return Array.from(new Set(statuses));
}

function normalizeTerm(value: string | null): string | undefined {
  const term = value?.trim();

  return term ? term : undefined;
}

function uniqueNonEmptyValues(values: string[]): string[] {
  return Array.from(
    new Set(values.map((value) => value.trim()).filter((value) => !!value))
  );
}

function getSprintId(value: string | null): number | undefined {
  if (!value) return undefined;

  const sprintId = Number(value);

  return Number.isFinite(sprintId) ? sprintId : undefined;
}

function normalizeStatusLabel(value: string): string {
  return value.trim().toLowerCase();
}
