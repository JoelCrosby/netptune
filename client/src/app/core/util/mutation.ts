import {
  DestroyRef,
  Signal,
  assertInInjectionContext,
  inject,
  signal,
} from '@angular/core';
import { getErrorMessage } from '@core/util/error-message';
import { Observable, finalize } from 'rxjs';

export interface MutationRunOptions<T> {
  fallbackError?: string;
  onSuccess?: (value: T) => void;
  onError?: (message: string, error: unknown) => void;
}

export interface Mutation {
  readonly pending: Signal<boolean>;
  readonly error: Signal<string | null>;
  run<T>(source: Observable<T>, options?: MutationRunOptions<T>): void;
  setError(message: string | null): void;
  clearError(): void;
}

export interface MutationOptions {
  fallbackError?: string;
}

// One mutation can serve several calls that share a busy flag and error banner;
// pass `fallbackError` per run when each call words its failure differently.
// Pipe ClientResponse calls through `requireSuccess()` so a failed response
// reaches the error branch. A request still in flight when the component goes away
// is left to finish, since cancelling could abandon a save the server already
// applied, but its callbacks are skipped.
export function mutation(options: MutationOptions = {}): Mutation {
  assertInInjectionContext(mutation);

  let destroyed = false;
  inject(DestroyRef).onDestroy(() => (destroyed = true));

  const pending = signal(false);
  const error = signal<string | null>(null);

  return {
    pending: pending.asReadonly(),
    error: error.asReadonly(),
    setError: (message) => error.set(message),
    clearError: () => error.set(null),
    run: (source, runOptions = {}) => {
      pending.set(true);
      error.set(null);

      source.pipe(finalize(() => pending.set(false))).subscribe({
        next: (value) => {
          if (destroyed) return;

          runOptions.onSuccess?.(value);
        },
        error: (caught: unknown) => {
          const message = getErrorMessage(
            caught,
            runOptions.fallbackError ?? options.fallbackError
          );

          error.set(message);

          if (destroyed) return;

          runOptions.onError?.(message, caught);
        },
      });
    },
  };
}
