import {
  assertInInjectionContext,
  computed,
  inject,
  Signal,
} from '@angular/core';
import { TaskFilterService } from '@core/services/task-filter.service';
import { TaskFilterRouteParams } from './task-filter-route-params';

export type TaskFilterKey = 'term' | 'users' | 'tags' | 'statusIds';

export interface TaskFilterRoute {
  /** The filters the views are currently narrowed to. */
  readonly filters: Signal<TaskFilterRouteParams>;
  readonly hasFilters: Signal<boolean>;
  set(key: TaskFilterKey, value: string | string[] | number[] | null): void;
  clear(): void;
}

/** The view-facing shape of {@link TaskFilterService}. */
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
        !!current.statuses?.length
      );
    }),
    set: (key, value) => taskFilters.update(toPatch(key, value)),
    clear: () => taskFilters.clear(),
  };
}

function toPatch(
  key: TaskFilterKey,
  value: string | string[] | number[] | null
): TaskFilterRouteParams {
  if (key === 'term') {
    return { term: (value as string | null) || null };
  }

  if (key === 'statusIds') {
    return { statuses: (value as number[]) ?? [] };
  }

  return { [key]: (value as string[]) ?? [] };
}
