import { ParamMap, Params } from '@angular/router';

export interface TaskFilterRouteParams {
  term?: string | null;
  tags?: string[];
  users?: string[];
  sprintId?: number;
  statuses?: number[];
  hasFlags?: boolean;
}

export interface TaskFilterRouteQueryParams extends Params {
  term?: string;
  tags?: string[];
  users?: string[];
  sprintId?: number;
  statusIds?: string[];
  hasFlags?: boolean;
}

export interface BuildTaskFilterRouteParamsOptions {
  includeStatuses?: boolean;
}

export function parseTaskFilterRouteParams(
  paramMap: ParamMap
): TaskFilterRouteParams {
  return {
    term: normalizeTerm(paramMap.get('term')),
    tags: uniqueNonEmptyValues(paramMap.getAll('tags')),
    users: uniqueNonEmptyValues(paramMap.getAll('users')),
    sprintId: getSprintId(paramMap.get('sprintId')),
    statuses: parseStatusIds(paramMap.getAll('statusIds')),
    hasFlags: paramMap.get('hasFlags') === 'true' ? true : undefined,
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
    queryParams['statusIds'] = statuses.map((status) => status.toString());
  }

  if (params.hasFlags) {
    queryParams['hasFlags'] = true;
  }

  return queryParams;
}

function parseStatusIds(values: string[]): number[] {
  const statuses = values
    .map((value) => Number(value))
    .filter((status) => Number.isInteger(status));

  return uniqueStatuses(statuses);
}

function uniqueStatuses(statuses: number[]): number[] {
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
