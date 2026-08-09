import { HttpResourceRequest } from '@angular/common/http';
import {
  assertInInjectionContext,
  computed,
  effect,
  linkedSignal,
  signal,
  Signal,
  untracked,
} from '@angular/core';
import { Permission } from '../auth/permissions';
import { DEFAULT_PAGE_SIZE } from '../models/pagination';
import { RefreshScope } from '../models/refresh-scope';
import { permissionResource } from './permission-resource';

/** Cursor-paged endpoints report where the next page starts in this header. */
const CURSOR_HEADER = 'X-Next-Cursor';

export type CursorResourceRequest = Omit<HttpResourceRequest, 'params'> & {
  params?: Record<string, string | number | boolean>;
};

export interface CursorResourceOptions<T> {
  /** Identity of a row, used so a page delivered twice is not shown twice. */
  trackBy: (item: T) => string | number;
  parse?: (response: unknown) => T[];
  pageSize?: number;
  refreshOn?: readonly RefreshScope[];
}

export interface CursorResourceRef<T> {
  /** Every page fetched so far, oldest request first. */
  readonly items: Signal<T[]>;
  /** False only while the first page of the current request is still on its way. */
  readonly loaded: Signal<boolean>;
  readonly isLoading: Signal<boolean>;
  readonly canLoadMore: Signal<boolean>;
  loadMore(): void;
}

/**
 * A `permissionResource` for an endpoint that pages by cursor, where the view stacks
 * the pages. Returning undefined from `request` idles it; changing it starts a new list.
 */
export function cursorResource<T>(
  request: () => CursorResourceRequest | undefined,
  permission: Permission,
  options: CursorResourceOptions<T>
): CursorResourceRef<T> {
  assertInInjectionContext(cursorResource);

  const pageSize = options.pageSize ?? DEFAULT_PAGE_SIZE;
  const base = computed(() => request());
  const listKey = computed(() => {
    const target = base();

    return target ? JSON.stringify(target) : null;
  });

  /* Keyed by the list it belongs to, so a new list cannot inherit the old one's cursor. */
  const cursor = signal<{ key: string; value: string } | null>(null);
  const activeCursor = computed(() => {
    const held = cursor();
    const isCurrent = held !== null && held.key === listKey();

    return isCurrent ? held.value : undefined;
  });

  const resource = permissionResource<T[]>(
    permission,
    () => {
      const target = base();

      if (!target) return undefined;

      const params: Record<string, string | number | boolean> = {
        ...target.params,
        take: pageSize,
      };
      const next = activeCursor();

      if (next) {
        params['cursor'] = next;
      }

      return { ...target, params };
    },
    {
      defaultValue: [],
      parse: options.parse,
      refreshOn: options.refreshOn,
    }
  );

  const items = linkedSignal<string | null, T[]>({
    source: listKey,
    computation: () => [],
  });

  effect(() => {
    const page = resource.value();

    if (!page.length) return;

    untracked(() => {
      items.update((shown) => append(shown, page, options.trackBy));
    });
  });

  const nextCursor = computed(() => {
    return resource.headers()?.get(CURSOR_HEADER) ?? undefined;
  });

  return {
    items: items.asReadonly(),
    isLoading: resource.isLoading,
    /* A later page must not put the loading state back over the pages already shown. */
    loaded: computed(() => hasSettled(resource) || items().length > 0),
    canLoadMore: computed(() => !!nextCursor()),
    loadMore: () => {
      const key = listKey();
      const next = nextCursor();

      if (key === null || !next) return;

      cursor.set({ key, value: next });
    },
  };
}

function hasSettled(resource: { status: Signal<string> }): boolean {
  const status = resource.status();

  return status === 'resolved' || status === 'error' || status === 'local';
}

function append<T>(
  shown: T[],
  page: readonly T[],
  trackBy: (item: T) => string | number
): T[] {
  const seen = new Set(shown.map(trackBy));
  const added = page.filter((item) => !seen.has(trackBy(item)));

  return added.length ? [...shown, ...added] : shown;
}
