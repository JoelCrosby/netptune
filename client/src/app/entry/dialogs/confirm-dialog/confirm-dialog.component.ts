import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { ThemePalette } from '@angular/material/core';

import { FormField, form } from '@angular/forms/signals';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
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
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatIcon,
    MatCheckbox,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormField,
  ],
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
