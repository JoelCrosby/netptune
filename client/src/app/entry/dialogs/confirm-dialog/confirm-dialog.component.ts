import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';

import { FormField, form } from '@angular/forms/signals';
import { CheckboxComponent } from '@app/static/components/checkbox/checkbox.component';
import { DialogContentComponent } from '@app/static/components/dialog-content/dialog-content.component';
import { LucideDynamicIcon, LucideIconInput } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';

export interface ConfirmDialogOptions {
  acceptLabel?: string;
  cancelLabel?: string;
  message?: string;
  messageExtended?: string;
  title?: string;
  confirmationCheckboxLabel?: string;
  color?: 'primary' | 'accent' | 'warn' | undefined;
  isInfoMessage?: boolean;
  icon?: LucideIconInput;
}

@Component({
  selector: 'app-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LucideDynamicIcon,
    CheckboxComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormField,
    DialogContentComponent,
  ],
  template: `<h1 class="mb-6 text-xl font-semibold">{{ data.title }}</h1>

    @if (data.message || data.confirmationCheckboxLabel) {
      <app-dialog-content>
        <div class="flex flex-col items-center">
          @if (data.icon) {
            <svg
              [lucideIcon]="data.icon"
              class="mx-auto my-[0.4rem] h-16 w-16"></svg>
          }
        </div>
        @if (data.message) {
          <p
            [innerHTML]="data.message"
            [class.m-0]="!data.messageExtended && !data.icon">
            {{ data.message }}
          </p>
          @if (data.messageExtended) {
            <p>{{ data.messageExtended }}</p>
          }
        }
        @if (data.confirmationCheckboxLabel) {
          <div class="bg-warn/20 border-warn/40 my-4 rounded border px-4 py-2">
            <app-checkbox [formField]="confirmForm.confirmationChecked">
              {{ data.confirmationCheckboxLabel }}
            </app-checkbox>
          </div>
        }
      </app-dialog-content>
    }

    <div app-dialog-actions align="end">
      @if (data.isInfoMessage) {
        <button
          app-stroked-button
          cdkFocusInitial
          (click)="dialogRef.close(false)">
          Ok
        </button>
      } @else {
        @if (data.cancelLabel) {
          <button
            app-stroked-button
            cdkFocusInitial
            (click)="dialogRef.close(false)">
            {{ data.cancelLabel }}
          </button>
        }
        <button
          app-flat-button
          [color]="data.color === 'warn' ? 'warn' : 'primary'"
          (click)="dialogRef.close(true)"
          [disabled]="
            !!data.confirmationCheckboxLabel &&
            !confirmForm.confirmationChecked().value()
          ">
          {{ data.acceptLabel }}
        </button>
      }
    </div> `,
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
