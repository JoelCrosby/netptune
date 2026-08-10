import { Injector, Signal, effect, signal, untracked } from '@angular/core';

export interface AnimatedPresence {
  isPresent: Signal<boolean>;
  isLeaving: Signal<boolean>;
}

export interface AnimatedPresenceOptions {
  injector?: Injector;
}

export function animatedPresence(
  source: Signal<boolean>,
  durationMs: number,
  options: AnimatedPresenceOptions = {}
): AnimatedPresence {
  const isPresent = signal(source());
  const isLeaving = signal(false);

  effect(
    (onCleanup) => {
      const isOpen = source();

      if (isOpen) {
        isLeaving.set(false);
        isPresent.set(true);

        return;
      }

      const wasPresent = untracked(isPresent);

      if (!wasPresent) {
        return;
      }

      isLeaving.set(true);

      const timeout = setTimeout(() => {
        isLeaving.set(false);
        isPresent.set(false);
      }, durationMs);

      onCleanup(() => clearTimeout(timeout));
    },
    { injector: options.injector }
  );

  return { isPresent, isLeaving };
}
