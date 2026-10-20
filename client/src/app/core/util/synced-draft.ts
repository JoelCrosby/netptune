import {
  assertInInjectionContext,
  computed,
  effect,
  Signal,
  signal,
  untracked,
  WritableSignal,
} from '@angular/core';

export interface SyncedDraftOptions<T> {
  // identifies what is being edited; a new key replaces the draft outright
  key: () => unknown;
  saved: () => T;
  isEditing: () => boolean;
  save: (value: T) => void;
  // puts a value from elsewhere into the control without it handing over its own unsaved edit
  replace?: (value: T) => void;
  equal?: (previous: T, current: T) => boolean;
}

export interface SyncedDraft<T> {
  readonly value: WritableSignal<T>;
  readonly hasIncoming: Signal<boolean>;
  save(value: T): void;
  loadIncoming(): void;
  keepMine(): void;
}

/**
 * Keeps a form control's draft in step with the saved value it edits. A save comes
 * back after the user may have typed on, so the control's own saves are echoes and
 * never touch the draft. A change from elsewhere fills the draft when the user is
 * idle, and is held for them to choose when they are mid edit.
 */
export function syncedDraft<T>(options: SyncedDraftOptions<T>): SyncedDraft<T> {
  assertInInjectionContext(syncedDraft);

  const equal = options.equal ?? Object.is;
  const draft = signal(untracked(options.saved));
  const incoming = signal<{ value: T } | null>(null);
  const sent: T[] = [];
  let shownKey: unknown = Symbol();

  const source = computed(
    () => ({ key: options.key(), value: options.saved() }),
    {
      equal: (previous, current) => {
        return (
          Object.is(previous.key, current.key) &&
          equal(previous.value, current.value)
        );
      },
    }
  );

  const fill = (value: T) => {
    options.replace?.(value);
    draft.set(value);
  };

  const receive = (key: unknown, value: T) => {
    const isOtherKey = !Object.is(key, shownKey);

    shownKey = key;

    if (isOtherKey) {
      sent.length = 0;
      incoming.set(null);
      fill(value);

      return;
    }

    const echo = sent.findIndex((item) => equal(item, value));

    if (echo >= 0) {
      sent.splice(echo, 1);

      return;
    }

    const isOnScreen = equal(value, draft());

    if (isOnScreen) {
      incoming.set(null);

      return;
    }

    const isChoosing = incoming() !== null;

    if (options.isEditing() || isChoosing) {
      incoming.set({ value });

      return;
    }

    draft.set(value);
  };

  effect(() => {
    const { key, value } = source();

    untracked(() => receive(key, value));
  });

  // while the user is choosing, a save would decide for them
  const save = (value: T) => {
    const isChoosing = untracked(incoming) !== null;
    const isSaved = equal(value, untracked(options.saved));
    const lastSent = sent.at(-1);
    const isInFlight = lastSent !== undefined && equal(lastSent, value);

    if (isChoosing || isSaved || isInFlight) return;

    sent.push(value);
    options.save(value);
  };

  const loadIncoming = () => {
    const held = incoming();

    if (held === null) return;

    incoming.set(null);
    fill(held.value);
  };

  const keepMine = () => {
    incoming.set(null);
    save(draft());
  };

  return {
    value: draft,
    hasIncoming: computed(() => incoming() !== null),
    save,
    loadIncoming,
    keepMine,
  };
}
