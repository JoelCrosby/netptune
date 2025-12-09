import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { ThemePalette } from '@angular/material/core';

import { Field, form } from '@angular/forms/signals';
import { MatButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatIcon } from '@angular/material/icon';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';

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
  imports: [MatIcon, MatCheckbox, DialogActionsDirective, MatButton, Field],
})
export class ConfirmDialogComponent {
  dialogRef = inject<DialogRef<boolean, ConfirmDialogComponent>>(DialogRef);
  data = inject<ConfirmDialogOptions>(DIALOG_DATA, { optional: true }) ?? {};

  confirmFormModel = signal({
    confirmationChecked: false,
  });

  confirmForm = form(this.confirmFormModel);

  constructor() {
    this.data = { color: 'primary', ...this.data };
  }
}
