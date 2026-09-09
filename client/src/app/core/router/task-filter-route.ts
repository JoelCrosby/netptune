import {
  assertInInjectionContext,
  computed,
  inject,
  Signal,
} from '@angular/core';
import { TaskFilterService } from '@core/services/task-filter.service';
import { TaskFilterRouteParams } from './task-filter-route-params';

export type TaskFilterKey =
  'term' | 'users' | 'tags' | 'statusIds' | 'hasTags' | 'hasFlags';

export type TaskFilterValue = string | string[] | number[] | boolean | null;

export interface TaskFilterRoute {
  readonly filters: Signal<TaskFilterRouteParams>;
  readonly hasFilters: Signal<boolean>;
  set(key: TaskFilterKey, value: TaskFilterValue): void;
  patch(filters: TaskFilterRouteParams): void;
  clear(): void;
}

export function taskFilterRoute(): TaskFilterRoute {
  assertInInjectionContext(taskFilterRoute);

  const taskFilters = inject(TaskFilterService);
  const filters = taskFilters.filters;

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
    set: (key, value) => taskFilters.update(toPatch(key, value)),
    patch: (filters) => taskFilters.update(filters),
    clear: () => taskFilters.clear(),
  };
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
