import { Injectable, inject } from '@angular/core';
import {
  ConfirmDialogComponent,
  ConfirmDialogOptions,
} from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { DialogService } from '@core/services/dialog.service';

const DEFAULT_CONFIG: ConfirmDialogOptions = {
  acceptLabel: 'Accept',
  cancelLabel: 'Cancel',
  title: 'Confirm ?',
  color: 'primary',
};

@Injectable({ providedIn: 'root' })
export class ConfirmationService {
  private dialog = inject(DialogService);

  open(
    config: ConfirmDialogOptions = DEFAULT_CONFIG,
    silent = false
  ): Observable<boolean> {
    if (silent) {
      return of(true);
    }

    const dialogRef = this.dialog.open<boolean>(ConfirmDialogComponent, {
      width: '600px',
      data: config,
    });

    return dialogRef.closed.pipe(map((value) => !!value));
  }
}
