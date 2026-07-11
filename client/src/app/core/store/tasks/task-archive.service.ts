import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { ConfirmationService } from '@core/services/confirmation.service';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { EMPTY, Observable } from 'rxjs';
import { map, switchMap, tap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class TaskArchiveService {
  private http = inject(HttpClient);
  private confirmation = inject(ConfirmationService);
  private snackbar = inject(SnackbarService);

  restore(groupId: string, ids: number[]): Observable<number[]> {
    return this.confirmation.open(buildRestoreConfirmation(ids.length)).pipe(
      switchMap((confirmed) => {
        if (!confirmed) return EMPTY;

        return this.http
          .post<ClientResponse>('api/tasks/restore', ids, {
            headers: { 'X-Group': groupId },
          })
          .pipe(
            unwrapClientReposne(),
            tap(() =>
              this.snackbar.open(
                ids.length === 1
                  ? 'Task restored'
                  : `${ids.length} tasks restored`
              )
            ),
            map(() => ids)
          );
      })
    );
  }
}

const buildRestoreConfirmation = (count: number): ConfirmDialogOptions => ({
  acceptLabel: 'Restore',
  cancelLabel: 'Cancel',
  message:
    count === 1
      ? 'Are you sure you want to restore this task?'
      : `Are you sure you want to restore these ${count} tasks?`,
  title: count === 1 ? 'Restore Task' : 'Restore Tasks',
});
