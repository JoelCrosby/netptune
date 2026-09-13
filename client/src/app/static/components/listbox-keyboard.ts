import { ActiveDescendantKeyManager, Highlightable } from '@angular/cdk/a11y';
import {
  DestroyRef,
  Injector,
  Signal,
  computed,
  inject,
  signal,
} from '@angular/core';

export interface ListboxKeyboardConfig<T extends Highlightable> {
  items: Signal<readonly T[]>;
  isOpen: () => boolean;
  open?: () => void;
  close: () => void;
  select: (item: T) => void;
  // Keys that open the list while it is closed.
  openKeys?: readonly string[];
  selectKeys?: readonly string[];
  // Swallowed while the list is open.
  trapKeys?: readonly string[];
  closeOnTab?: boolean;
  wrap?: boolean;
  homeAndEnd?: boolean;
  horizontal?: boolean;
  typeahead?: boolean;
  skip?: (item: T) => boolean;
  // Runs when a key moved the active option, not when a pointer did.
  navigated?: (index: number) => void;
}

export interface ListboxOption<V> extends Highlightable {
  readonly value: V;
}

const escapeKeys = ['Escape', 'Esc'];

// Keyboard handling for a combobox whose focus stays on its input while
// aria-activedescendant points at the active option.
export class ListboxKeyboard<T extends Highlightable> {
  private readonly injector = inject(Injector);
  private readonly index = signal(-1);
  private keyManager?: ActiveDescendantKeyManager<T>;

  readonly activeIndex = this.index.asReadonly();

  constructor(private readonly config: ListboxKeyboardConfig<T>) {
    inject(DestroyRef).onDestroy(() => this.keyManager?.destroy());
  }

  get activeItem(): T | null {
    return this.config.items()[this.index()] ?? null;
  }

  handleKeydown(event: KeyboardEvent) {
    const key = event.key;

    if (!this.config.isOpen()) {
      const opens = this.config.openKeys?.includes(key) ?? false;

      if (opens) {
        event.preventDefault();
        this.config.open?.();
      }

      return;
    }

    if (escapeKeys.includes(key)) {
      event.preventDefault();
      this.config.close();

      return;
    }

    const selectKeys = this.config.selectKeys ?? ['Enter'];

    if (selectKeys.includes(key)) {
      event.preventDefault();
      this.selectActive();

      return;
    }

    const trapped = this.config.trapKeys?.includes(key) ?? false;

    if (trapped) {
      event.preventDefault();

      return;
    }

    const before = this.index();

    this.manager().onKeydown(event);
    this.sync();

    const after = this.index();

    if (after !== before) {
      this.config.navigated?.(after);
    }
  }

  setActiveIndex(index: number) {
    this.manager().setActiveItem(index);
    this.sync();
  }

  setActiveItem(item: T) {
    this.manager().setActiveItem(item);
    this.sync();
  }

  setFirstItemActive() {
    this.manager().setFirstItemActive();
    this.sync();
  }

  clearActive() {
    this.setActiveIndex(-1);
  }

  private selectActive() {
    const active = this.activeItem;

    if (!active) return;

    this.config.select(active);
  }

  private sync() {
    this.index.set(this.manager().activeItemIndex ?? -1);
  }

  // Built on first use because reading the items (which typeahead does straight
  // away) throws while they still depend on required inputs.
  private manager(): ActiveDescendantKeyManager<T> {
    if (this.keyManager) return this.keyManager;

    const config = this.config;
    const manager = new ActiveDescendantKeyManager(config.items, this.injector)
      .withVerticalOrientation()
      .withWrap(config.wrap ?? false)
      .withHomeAndEnd(config.homeAndEnd ?? false);

    if (config.horizontal) {
      manager.withHorizontalOrientation('ltr');
    }

    if (config.skip) {
      manager.skipPredicate(config.skip);
    }

    if (config.typeahead) {
      manager.withTypeAhead();
    }

    if (config.closeOnTab) {
      manager.tabOut.subscribe(() => config.close());
    }

    manager.change.subscribe(() => this.sync());

    this.keyManager = manager;

    return manager;
  }
}

// Wraps plain data so the key manager can move over it; the active option is
// rendered from `activeIndex` rather than through highlight styles.
export function listboxOptions<V>(
  source: Signal<readonly V[]>,
  labelOf?: (value: V) => string
): Signal<ListboxOption<V>[]> {
  return computed(() => {
    return source().map((value) => toListboxOption(value, labelOf));
  });
}

function toListboxOption<V>(
  value: V,
  labelOf?: (value: V) => string
): ListboxOption<V> {
  return {
    value,
    getLabel: labelOf ? () => labelOf(value) : undefined,
    setActiveStyles: () => undefined,
    setInactiveStyles: () => undefined,
  };
}
