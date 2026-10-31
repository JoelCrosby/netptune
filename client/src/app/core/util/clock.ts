import {
  DestroyRef,
  Signal,
  assertInInjectionContext,
  inject,
  signal,
} from '@angular/core';

export function clock(tickMs: number): Signal<Date> {
  assertInInjectionContext(clock);

  const now = signal(new Date());
  const timer = setInterval(() => now.set(new Date()), tickMs);

  inject(DestroyRef).onDestroy(() => clearInterval(timer));

  return now.asReadonly();
}
