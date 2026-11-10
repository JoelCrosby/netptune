import { Component, input, model, output } from '@angular/core';
import {
  FormField,
  form,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { toDateInputValue } from '@core/util/dates';
import { DialogActionsDirective } from '../../directives/dialog-actions.directive';
import { FlatButtonComponent } from '../button/flat-button.component';
import { StrokedButtonComponent } from '../button/stroked-button.component';
import { FormErrorComponent } from '../form-error/form-error.component';
import { FormInputComponent } from '../form-input/form-input.component';

export interface AccessTokenFormValue {
  account: string;
  token: string;
  expiresOn: string;
}

export const emptyAccessTokenFormValue: AccessTokenFormValue = {
  account: '',
  token: '',
  expiresOn: '',
};

const REQUIRED_MESSAGE = $localize`:Validation error when a required field of the access token form is empty:This field is required.`;
const EXPIRY_PASSED_MESSAGE = $localize`:Validation error when the expiry date of an access token has already passed:The expiry date has already passed.`;

@Component({
  selector: 'app-access-token-form',
  imports: [
    DialogActionsDirective,
    FlatButtonComponent,
    FormErrorComponent,
    FormField,
    FormInputComponent,
    StrokedButtonComponent,
  ],
  template: `
    <form class="flex flex-col" (submit)="onSubmit($event)">
      <app-form-input
        [label]="accountLabel()"
        [placeholder]="accountPlaceholder()"
        [formField]="tokenForm.account" />

      <app-form-input
        type="password"
        [label]="tokenLabel()"
        [formField]="tokenForm.token" />

      <app-form-input
        type="date"
        [label]="expiryLabel()"
        [hint]="expiryHint()"
        [formField]="tokenForm.expiresOn" />

      <ng-content />

      @if (error(); as message) {
        <app-form-error role="alert">{{ message }}</app-form-error>
      }

      <div app-dialog-actions align="end">
        <button app-stroked-button type="button" (click)="cancelled.emit()">
          <span i18n="Dismisses a dialog without acting">Cancel</span>
        </button>
        <button
          app-flat-button
          color="primary"
          type="submit"
          [disabled]="pending()">
          {{ submitLabel() }}
        </button>
      </div>
    </form>
  `,
})
export class AccessTokenFormComponent {
  readonly value = model<AccessTokenFormValue>(emptyAccessTokenFormValue);
  readonly accountLabel = input.required<string>();
  readonly accountPlaceholder = input<string | null>(null);
  readonly tokenLabel = input(
    $localize`:Label of the field for an access token used to connect another service:Access token`
  );
  readonly expiryLabel = input(
    $localize`:Label of the optional field for the date an access token expires:Expiry date`
  );
  readonly expiryHint = input<string | null>(null);
  readonly submitLabel = input.required<string>();
  readonly error = input<string | null>(null);
  readonly pending = input(false);

  readonly submitted = output<AccessTokenFormValue>();
  readonly cancelled = output();

  protected readonly tokenForm = form(this.value, (schema) => {
    required(schema.account, { message: REQUIRED_MESSAGE });
    required(schema.token, { message: REQUIRED_MESSAGE });
    validate(schema.expiresOn, ({ value }) => {
      const expiresOn = value();
      const hasPassed = !!expiresOn && expiresOn < toDateInputValue(new Date());

      if (!hasPassed) {
        return undefined;
      }

      return { kind: 'expiryPassed', message: EXPIRY_PASSED_MESSAGE };
    });
  });

  protected onSubmit(event: Event) {
    event.preventDefault();

    submit(this.tokenForm, async () => {
      const value = this.value();

      this.submitted.emit({
        account: value.account.trim(),
        token: value.token.trim(),
        expiresOn: value.expiresOn,
      });
    });
  }
}
