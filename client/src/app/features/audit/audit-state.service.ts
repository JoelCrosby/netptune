import { inject } from '@angular/core';
import {
  AuditActivityPoint,
  AuditLogFilter,
} from '@core/models/view-models/audit-log-view-model';
import { AuditService } from '@core/store/audit/audit.service';
import { Logger } from '@core/util/logger';
import { tapResponse } from '@ngrx/operators';
import {
  patchState,
  signalStore,
  withHooks,
  withMethods,
  withState,
} from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap } from 'rxjs';

interface AuditState {
  filter: AuditLogFilter;
  summary: AuditActivityPoint[];
  loaded: boolean;
}

const initialState: AuditState = {
  filter: {},
  summary: [],
  loaded: false,
};

export const AuditStore = signalStore(
  withState(initialState),
  withMethods((store, auditService = inject(AuditService)) => ({
    loadSummary: rxMethod<AuditLogFilter>(
      pipe(
        switchMap((filter) =>
          auditService.getActivitySummary(filter).pipe(
            tapResponse({
              next: (response) => {
                return patchState(store, {
                  summary: response.payload ?? [],
                  loaded: true,
                });
              },
              error: (err) => {
                Logger.error(err);
                patchState(store, { loaded: true });
              },
            })
          )
        )
      )
    ),
    applyFilters(from?: string, to?: string) {
      patchState(store, {
        filter: { from, to },
      });
    },
    reset() {
      patchState(store, { filter: {} });
    },
  })),
  withHooks({
    onInit: (store) => {
      store.loadSummary(store.filter);
    },
  })
);
