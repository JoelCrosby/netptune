import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { MatLegacyDialog as MatDialog } from '@angular/material/legacy-dialog';
import {
  ConfirmDialogComponent,
  ConfirmDialogOptions,
} from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { map } from 'rxjs/operators';

const DEFAULT_CONFIG: ConfirmDialogOptions = {
  acceptLabel: 'Accept',
  cancelLabel: 'Cancel',
  title: 'Confirm ?',
  color: 'primary',
};

@Injectable({ providedIn: 'root' })
export class ConfirmationService {
  constructor(private dialog: MatDialog) {}

  open(
    config: ConfirmDialogOptions = DEFAULT_CONFIG,
    silent = false
  ): Observable<boolean> {
    if (silent) {
      return of(true);
    }

    const dialogRef = this.dialog.open<
      ConfirmDialogComponent,
      ConfirmDialogOptions,
      boolean
    >(ConfirmDialogComponent, {
      width: '600px',
      data: config,
    });

    return dialogRef.afterClosed().pipe(map((value) => !!value));
  }
}
