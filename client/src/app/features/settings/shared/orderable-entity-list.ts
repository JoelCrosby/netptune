import { Signal, computed, signal } from '@angular/core';
import { Params } from '@angular/router';
import { ClientResponse } from '@core/models/client-response';
import { mutation } from '@core/util/mutation';
import { requireSuccess } from '@core/util/rxjs-operators';
import { debouncedSignal, onChange, reloadToken } from '@core/util/signals';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableSort } from '@static/components/datatable/datatable.types';
import { Observable } from 'rxjs';

// Call in a field initialiser, after the datatable viewChild it is handed.
export function searchableEntityList<T>(
  table: Signal<DatatableComponent<T> | undefined>
) {
  const searchInput = signal('');
  const search = debouncedSignal(searchInput);
  const sort = signal<DatatableSort | null>(null);

  const searchParams = computed<Params>(() => {
    const term = search().trim();

    return term ? { search: term } : {};
  });

  onChange(search, () => table()?.goToPage(1));

  return { searchInput, search, sort, searchParams };
}

export function orderableEntityList<T>(
  table: Signal<DatatableComponent<T> | undefined>
) {
  const list = searchableEntityList(table);
  const request = mutation();
  const token = reloadToken();

  const moveUpLabel = $localize`:Tooltip on the button that moves a row up:Move up`;
  const moveDownLabel = $localize`:Tooltip on the button that moves a row down:Move down`;
  const manualOrderOnlyLabel = $localize`:Explains that manual reordering needs the default, unfiltered view:Clear the search and sort by Order to reorder`;

  // Rows only sit next to their sort-order neighbours in the default, unfiltered view,
  // so that is the only view where moving a row up or down means anything.
  const manualOrderActive = computed(() => {
    const sort = list.sort();
    const sortedByOrder = sort === null || sort.sortBy === 'sortOrder';

    return sortedByOrder && !list.search().trim();
  });

  const globalIndex = (rowIndex: number) => {
    const current = table();

    if (!current) return rowIndex;

    return (current.currentPage() - 1) * current.pageSize() + rowIndex;
  };

  const reload = () => {
    request.clearError();
    token.bump();
  };

  return {
    ...list,
    pending: request.pending,
    error: request.error,
    reloadToken: token,
    manualOrderActive,
    moveUpLabel,
    moveDownLabel,
    reload,

    moveTooltip: (label: string) =>
      manualOrderActive() ? label : manualOrderOnlyLabel,

    canMoveUp: (rowIndex: number) => {
      const isFirstOverall = globalIndex(rowIndex) === 0;

      return manualOrderActive() && !request.pending() && !isFirstOverall;
    },

    canMoveDown: (rowIndex: number) => {
      const isLastOverall =
        globalIndex(rowIndex) === (table()?.totalCount() ?? 0) - 1;

      return manualOrderActive() && !request.pending() && !isLastOverall;
    },

    save: (source: Observable<ClientResponse<unknown>>, fallback: string) => {
      request.run(source.pipe(requireSuccess()), {
        fallbackError: fallback,
        onSuccess: reload,
      });
    },

    // A create that succeeds without returning the new row still counts as a failure.
    create: (source: Observable<ClientResponse<unknown>>, fallback: string) => {
      request.run(source.pipe(requireSuccess()), {
        fallbackError: fallback,
        onSuccess: (response) => {
          if (!response.payload) {
            request.setError(response.message ?? fallback);

            return;
          }

          reload();
        },
      });
    },
  };
}
