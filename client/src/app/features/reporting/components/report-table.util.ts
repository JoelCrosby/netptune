import { Signal, effect } from '@angular/core';
import { Params } from '@angular/router';

interface Pageable {
  goToPage(page: number): void;
}

export function queryParams(query: Signal<string>): () => Params {
  return () => Object.fromEntries(new URLSearchParams(query()));
}

export function resetPageOnFilterChange(
  params: Signal<Params>,
  datatable: Signal<Pageable | undefined>
): void {
  let previous: string | null = null;

  effect(() => {
    const current = JSON.stringify(params());
    const isFirstRun = previous === null;
    const hasChanged = current !== previous;

    previous = current;

    if (isFirstRun || !hasChanged) return;

    datatable()?.goToPage(1);
  });
}
