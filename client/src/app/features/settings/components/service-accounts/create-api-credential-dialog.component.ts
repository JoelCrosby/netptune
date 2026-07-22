import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import {
  apply,
  FormField,
  form,
  submit as submitForm,
} from '@angular/forms/signals';
import { Permission } from '@core/auth/permissions';
import {
  CreateApiCredentialRequest,
  ServiceAccount,
} from '@core/models/service-account';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { permissionLabel } from './service-account-permissions';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

@Component({
  selector: 'app-create-api-credential-dialog',
  imports: [
    FormField,
    FormInputComponent,
    CheckboxComponent,
    DialogTitleComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `<app-dialog-title>Create API Credential</app-dialog-title>

    <form app-dialog-content (submit)="submit($event)">
      <p class="text-muted mb-5 text-sm">
        Create a credential for <strong>{{ account.name }}</strong
        >. The secret is displayed once and expires automatically after 90 days.
      </p>

      <app-form-input
        [formField]="credentialForm.name"
        label="Credential name"
        placeholder="Local Codex"
        hint="Describe where this credential will be used."
        maxLength="128" />

      <fieldset class="mt-2">
        <legend class="mb-1 text-sm font-medium">Credential scopes</legend>
        <p class="text-muted mb-3 text-xs">
          Scopes can restrict this credential further than the service account.
        </p>

        <div class="border-border divide-border divide-y rounded border">
          @for (permission of account.permissions; track permission) {
            <div class="px-4 py-3">
              <app-checkbox
                [checked]="hasScope(permission)"
                (changed)="setScope(permission, $event)">
                <span class="text-sm">{{
                  getPermissionLabel(permission)
                }}</span>
              </app-checkbox>
            </div>
          } @empty {
            <p class="text-muted px-4 py-3 text-sm">
              This service account has no API permissions.
            </p>
          }
        </div>
      </fieldset>
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">Cancel</button>
      <button
        app-flat-button
        type="button"
        [disabled]="selectedScopes().size === 0"
        (click)="submit($event)">
        Create Credential
      </button>
    </div>`,
})
export class CreateApiCredentialDialogComponent {
  private readonly dialogRef =
    inject<
      DialogRef<CreateApiCredentialRequest, CreateApiCredentialDialogComponent>
    >(DialogRef);

  readonly account = inject<ServiceAccount>(DIALOG_DATA);
  readonly selectedScopes = signal<Set<Permission>>(
    new Set(this.account.permissions)
  );

  readonly credentialFormModel = signal({ name: '' });
  readonly credentialForm = form(this.credentialFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({
        label: 'Credential name',
        minLength: 2,
        maxLength: 128,
      })
    );
  });

  hasScope(permission: Permission) {
    return this.selectedScopes().has(permission);
  }

  setScope(permission: Permission, selected: boolean) {
    this.selectedScopes.update((current) => {
      const next = new Set(current);
      if (selected) {
        next.add(permission);
      } else {
        next.delete(permission);
      }
      return next;
    });
  }

  getPermissionLabel(permission: Permission) {
    return permissionLabel(permission);
  }

  submit(event: Event) {
    event.preventDefault();

    if (this.selectedScopes().size === 0) return;

    submitForm(this.credentialForm, async () => {
      this.dialogRef.close({
        name: this.credentialForm.name().value().trim(),
        scopes: [...this.selectedScopes()],
      });
    });
  }
}
