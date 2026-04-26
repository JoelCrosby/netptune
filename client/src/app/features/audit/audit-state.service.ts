import { inject } from '@angular/core';
import { tapResponse } from '@ngrx/operators';
import {
  patchState,
  signalStore,
  withComputed,
  withHooks,
  withMethods,
  withState,
} from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { computed } from '@angular/core';
import { pipe, switchMap, tap } from 'rxjs';
import {
  AuditActivityPoint,
  AuditLogFilter,
  AuditLogViewModel,
} from '@core/models/view-models/audit-log-view-model';
import { AuditService } from '@core/store/audit/audit.service';

interface AuditState {
  filter: AuditLogFilter;
  items: AuditLogViewModel[];
  summary: AuditActivityPoint[];
  totalCount: number;
  totalPages: number;
  loading: boolean;
  error: boolean;
}

const initialState: AuditState = {
  filter: { page: 1, pageSize: 50 },
  items: [],
  summary: [],
  totalCount: 0,
  totalPages: 1,
  loading: false,
  error: false,
};

export const AuditStore = signalStore(
  withState(initialState),
  withComputed(({ filter }) => ({
    currentPage: computed(() => filter().page ?? 1),
  })),
  withMethods((store, auditService = inject(AuditService)) => ({
    load: rxMethod<AuditLogFilter>(
      pipe(
        tap(() => patchState(store, { loading: true, error: false })),
        switchMap((filter) =>
          auditService.getAuditLog(filter).pipe(
            tapResponse({
              next: (response) => {
                const page = response.payload ?? null;
                patchState(store, {
                  items: page?.items ?? [],
                  totalCount: page?.totalCount ?? 0,
                  totalPages: page?.totalPages ?? 1,
                  loading: false,
                });
              },
              error: () => patchState(store, { loading: false, error: true }),
            })
          )
        )
      )
    ),
    loadSummary: rxMethod<AuditLogFilter>(
      pipe(
        switchMap((filter) =>
          auditService.getActivitySummary(filter).pipe(
            tapResponse({
              next: (response) =>
                patchState(store, { summary: response.payload ?? [] }),
              error: () => {},
            })
          )
        )
      )
    ),
    applyFilters(from?: string, to?: string) {
      patchState(store, { filter: { from, to, page: 1, pageSize: 50 } });
    },
    reset() {
      patchState(store, { filter: { page: 1, pageSize: 50 } });
    },
    goToPage(page: number) {
      patchState(store, { filter: { ...store.filter(), page } });
    },
  })),
  withHooks({
    onInit: (store) => {
      store.load(store.filter);
      store.loadSummary(store.filter);
    },
  })
);
