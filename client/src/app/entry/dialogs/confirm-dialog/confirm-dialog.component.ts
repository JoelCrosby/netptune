import { DialogRef, DIALOG_DATA } from '@angular/cdk/dialog';
import {
  Component,
  Inject,
  Optional,
  ChangeDetectionStrategy,
} from '@angular/core';
import { ThemePalette } from '@angular/material/core';

export interface ConfirmDialogOptions {
  acceptLabel?: string;
  cancelLabel?: string;
  message?: string;
  messageExtended?: string;
  title?: string;
  confirmationCheckboxLabel?: string;
  color?: ThemePalette;
  isInfoMessage?: boolean;
  icon?: string;
}

@Component({
    selector: 'app-confirm-dialog',
    templateUrl: './confirm-dialog.component.html',
    styleUrls: ['./confirm-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class ConfirmDialogComponent {
  confirmationChecked = false;

  constructor(
    public dialogRef: DialogRef<boolean, ConfirmDialogComponent>,
    @Optional() @Inject(DIALOG_DATA) public data: ConfirmDialogOptions = {}
  ) {
    this.data = { color: 'primary', ...this.data };
  }
}
