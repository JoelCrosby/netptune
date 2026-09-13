import {
  Signal,
  WritableSignal,
  assertInInjectionContext,
  effect,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { debounceTime } from 'rxjs';

export function debouncedSignal<T>(source: Signal<T>, ms = 250): Signal<T> {
  assertInInjectionContext(debouncedSignal);

  return toSignal(toObservable(source).pipe(debounceTime(ms)), {
    initialValue: untracked(source),
  });
}

export type ReloadToken = Signal<number> & { bump(): void };

// Hand to anything that reloads when a signal changes, such as a datatable's `reloadSignal`.
export function reloadToken(): ReloadToken {
  const token = signal(0);

  return Object.assign(token.asReadonly(), {
    bump: () => token.update((value) => value + 1),
  });
}

// The first run is the value the caller already has, so only later ones are changes.
export function onChange<T>(
  source: Signal<T>,
  onChanged: (value: T) => void
): void {
  assertInInjectionContext(onChange);

  let isFirstRun = true;

  effect(() => {
    const value = source();

    if (isFirstRun) {
      isFirstRun = false;

      return;
    }

    untracked(() => onChanged(value));
  });
}

export function toggleInSet<K>(
  target: WritableSignal<ReadonlySet<K>>,
  key: K,
  on?: boolean
): void {
  target.update((current) => {
    const include = on ?? !current.has(key);

    if (include === current.has(key)) return current;

    const next = new Set(current);

    if (include) {
      next.add(key);
    } else {
      next.delete(key);
    }

    return next;
  });
}
