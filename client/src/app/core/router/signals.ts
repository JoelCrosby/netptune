import {
  Signal,
  assertInInjectionContext,
  computed,
  inject,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { map } from 'rxjs';

export function injectParams(): Signal<ParamMap> {
  assertInInjectionContext(injectParams);
  const route = inject(ActivatedRoute);

  return toSignal(route.queryParamMap, { requireSync: true });
}

export function injectParam<T>(
  keyOrTransform: string | ((queryParamMap: ParamMap) => T)
): Signal<T | null> {
  assertInInjectionContext(injectParams);
  const route = inject(ActivatedRoute);

  if (typeof keyOrTransform === 'function') {
    return toSignal(route.queryParamMap.pipe(map(keyOrTransform)), {
      requireSync: true,
    });
  }

  const getParam = (queryParamMap: ParamMap) => {
    return (queryParamMap?.get(keyOrTransform) as T) ?? null;
  };

  return toSignal(route.queryParamMap.pipe(map(getParam)), {
    requireSync: true,
  });
}

export const selectedTags = () => {
  return computed(() => {
    const values = injectParam<string[]>('tags');
    return new Set(values());
  });
};
