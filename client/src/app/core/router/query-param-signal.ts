import {
  Signal,
  assertInInjectionContext,
  computed,
  inject,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  ActivatedRoute,
  NavigationExtras,
  ParamMap,
  Router,
} from '@angular/router';

export type QueryParamValue = string | number | boolean | null;

export type QueryParamExtras = Omit<
  NavigationExtras,
  'relativeTo' | 'queryParams' | 'queryParamsHandling'
>;

export interface QueryParamsRoute {
  readonly paramMap: Signal<ParamMap>;
  // Merges into the current query string; a null value removes that param.
  patch(
    params: Record<string, QueryParamValue>,
    extras?: QueryParamExtras
  ): void;
}

export interface QueryParamSignal<T> extends Signal<T> {
  set(value: QueryParamValue, extras?: QueryParamExtras): void;
}

export function queryParamsRoute(): QueryParamsRoute {
  assertInInjectionContext(queryParamsRoute);

  const route = inject(ActivatedRoute);
  const router = inject(Router);

  return {
    paramMap: toSignal(route.queryParamMap, { requireSync: true }),
    patch: (params, extras = {}) => {
      void router.navigate([], {
        ...extras,
        relativeTo: route,
        queryParams: params,
        queryParamsHandling: 'merge',
      });
    },
  };
}

export function queryParamSignal<T>(
  name: string,
  parse: (value: string | null) => T
): QueryParamSignal<T> {
  assertInInjectionContext(queryParamSignal);

  const queryParams = queryParamsRoute();
  const value = computed(() => parse(queryParams.paramMap().get(name)));

  return Object.assign(value, {
    set: (next: QueryParamValue, extras?: QueryParamExtras) => {
      queryParams.patch({ [name]: next }, extras);
    },
  });
}

export function positiveIntegerParam(value: string | null): number | undefined {
  const parsed = Number(value);

  return Number.isInteger(parsed) && parsed > 0 ? parsed : undefined;
}
