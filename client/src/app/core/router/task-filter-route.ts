import {
  assertInInjectionContext,
  computed,
  inject,
  Signal,
} from '@angular/core';
import { Params } from '@angular/router';
import { TaskFilterService } from '@core/services/task-filter.service';
import { TaskFilterRouteParams } from './task-filter-route-params';

export type TaskFilterKey =
  'term' | 'users' | 'tags' | 'statusIds' | 'hasTags' | 'hasFlags';

export type TaskFilterValue = string | string[] | number[] | boolean | null;

export interface TaskFilterRoute {
  readonly filters: Signal<TaskFilterRouteParams>;
  readonly hasFilters: Signal<boolean>;
  readonly requestParams: Signal<Params>;
  readonly isFiltered: Signal<boolean>;
  set(key: TaskFilterKey, value: TaskFilterValue): void;
  patch(filters: TaskFilterRouteParams): void;
  clear(): void;
}

export function taskFilterRoute(): TaskFilterRoute {
  assertInInjectionContext(taskFilterRoute);

  const taskFilters = inject(TaskFilterService);
  const filters = taskFilters.filters;

  const requestParams = computed(() => toRequestParams(filters()), {
    equal: (previous, current) => {
      return JSON.stringify(previous) === JSON.stringify(current);
    },
  });

  return {
    filters,
    hasFilters: computed(() => {
      const current = filters();

      return (
        !!current.term ||
        !!current.tags?.length ||
        !!current.users?.length ||
        !!current.statuses?.length ||
        current.hasTags !== undefined ||
        current.hasFlags === true
      );
    }),
    requestParams,
    isFiltered: computed(() => Object.keys(requestParams()).length > 0),
    set: (key, value) => taskFilters.update(toPatch(key, value)),
    patch: (filters) => taskFilters.update(filters),
    clear: () => taskFilters.clear(),
  };
}

function toRequestParams(filters: TaskFilterRouteParams): Params {
  const params: Params = {};
  const search = filters.term?.trim();

  if (search) {
    params['search'] = search;
  }

  if (filters.tags?.length) {
    params['tags'] = filters.tags;
  }

  if (filters.users?.length) {
    params['assignees'] = filters.users;
  }

  if (filters.statuses?.length) {
    params['statusIds'] = filters.statuses;
  }

  if (filters.projectId !== undefined) {
    params['projectId'] = filters.projectId;
  }

  if (filters.sprintId !== undefined) {
    params['sprintId'] = filters.sprintId;
  }

  if (filters.hasTags !== undefined) {
    params['hasTags'] = filters.hasTags;
  }

  if (filters.hasFlags !== undefined) {
    params['hasFlags'] = filters.hasFlags;
  }

  return params;
}

function toPatch(
  key: TaskFilterKey,
  value: TaskFilterValue
): TaskFilterRouteParams {
  if (key === 'term') {
    return { term: (value as string | null) || null };
  }

  if (key === 'statusIds') {
    return { statuses: (value as number[]) ?? [] };
  }

  if (key === 'hasTags' || key === 'hasFlags') {
    return { [key]: (value as boolean | null) ?? undefined };
  }

  return { [key]: (value as string[]) ?? [] };
}
