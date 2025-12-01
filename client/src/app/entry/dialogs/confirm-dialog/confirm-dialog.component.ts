import { DialogRef, DIALOG_DATA } from '@angular/cdk/dialog';
import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { ThemePalette } from '@angular/material/core';

import { MatIcon } from '@angular/material/icon';
import { MatCheckbox } from '@angular/material/checkbox';
import { FormsModule } from '@angular/forms';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { MatButton } from '@angular/material/button';

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
  imports: [
    MatIcon,
    MatCheckbox,
    FormsModule,
    DialogActionsDirective,
    MatButton,
  ],
})
export class ConfirmDialogComponent {
  dialogRef = inject<DialogRef<boolean, ConfirmDialogComponent>>(DialogRef);
  data = inject<ConfirmDialogOptions>(DIALOG_DATA, { optional: true }) ?? {};

  confirmationChecked = false;

  constructor() {
    this.data = { color: 'primary', ...this.data };
  }
}
