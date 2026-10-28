import {
  DestroyRef,
  Signal,
  assertInInjectionContext,
  inject,
  signal,
} from '@angular/core';

// The clock is only ever read to the second, so it ticks no faster.
const ELAPSED_TICK = 1000;

export interface ElapsedClock {
  readonly elapsedMs: Signal<number>;
  start(startedAt?: number): void;
  stop(): number;
  reset(): void;
}

export function elapsedClock(): ElapsedClock {
  assertInInjectionContext(elapsedClock);

  const elapsedMs = signal(0);

  let startedAt: number | null = null;
  let timer: ReturnType<typeof setInterval> | null = null;

  const stopTimer = () => {
    if (timer === null) return;

    clearInterval(timer);
    timer = null;
  };

  inject(DestroyRef).onDestroy(stopTimer);

  return {
    elapsedMs: elapsedMs.asReadonly(),
    start(at = Date.now()) {
      stopTimer();

      startedAt = at;
      elapsedMs.set(since(at));

      timer = setInterval(() => elapsedMs.set(since(at)), ELAPSED_TICK);
    },
    stop() {
      stopTimer();

      if (startedAt === null) return 0;

      const elapsed = since(startedAt);

      startedAt = null;
      elapsedMs.set(elapsed);

      return elapsed;
    },
    reset() {
      elapsedMs.set(0);
    },
  };
}

function since(startedAt: number): number {
  return Math.max(0, Date.now() - startedAt);
}
