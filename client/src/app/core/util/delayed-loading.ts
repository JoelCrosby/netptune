import { Injector, Signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of, timer } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';

export const SKELETON_DELAY_MS = 200;

export interface DelayedLoadingOptions {
  delayMs?: number;
  injector?: Injector;
}

/**
 * Loading state that only turns true once the source has been loading for
 * `delayMs`, so fast responses never flash a placeholder.
 */
export function delayedLoading(
  source: Signal<boolean>,
  options: DelayedLoadingOptions = {}
): Signal<boolean> {
  const delayMs = options.delayMs ?? SKELETON_DELAY_MS;

  return toSignal(
    toObservable(source, { injector: options.injector }).pipe(
      switchMap((isLoading) => {
        if (!isLoading) return of(false);

        return timer(delayMs).pipe(map(() => true));
      })
    ),
    { initialValue: false, injector: options.injector }
  );
}
