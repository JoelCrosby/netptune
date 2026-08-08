import {
  assertInInjectionContext,
  computed,
  inject,
  Signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import {
  parseTaskFilterRouteParams,
  TaskFilterRouteParams,
} from './task-filter-route-params';

export type TaskFilterKey = 'term' | 'users' | 'tags' | 'statusIds';

export interface TaskFilterRoute {
  readonly filters: Signal<TaskFilterRouteParams>;
  readonly hasFilters: Signal<boolean>;
  set(key: TaskFilterKey, value: string | string[] | number[] | null): void;
  clear(): void;
}

export function taskFilterRoute(): TaskFilterRoute {
  assertInInjectionContext(taskFilterRoute);

  const router = inject(Router);
  const route = inject(ActivatedRoute);

  const params = toSignal(route.queryParamMap, {
    initialValue: route.snapshot.queryParamMap,
  });

  const filters = computed(() => parseTaskFilterRouteParams(params()));

  const navigate = (queryParams: Record<string, unknown>) => {
    void router.navigate([], {
      relativeTo: route,
      queryParams,
      queryParamsHandling: 'merge',
    });
  };

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
    set: (key, value) => {
      const hasValue = Array.isArray(value) ? value.length > 0 : !!value;

      navigate({ [key]: hasValue ? value : null });
    },
    clear: () => {
      navigate({ term: null, users: null, tags: null, statusIds: null });
    },
  };
}
