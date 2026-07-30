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
  template: `
    <app-dialog-title i18n="Title of the create-API-credential dialog">
      Create API Credential
    </app-dialog-title>

    <form app-dialog-content (submit)="submit($event)">
      <p class="text-muted mb-5 text-sm">
        <span
          i18n="
            Explains credential creation. NAME is the service account name and
            the 90 day expiry is fixed by the server
          ">
          Create a credential for <strong>{{ account.name }}</strong
          >. The secret is displayed once and expires automatically after 90
          days.
        </span>
      </p>

      <app-form-input
        [formField]="credentialForm.name"
        i18n-label="Label of the credential name field"
        label="Credential name"
        i18n-placeholder="Example credential name shown as placeholder text"
        placeholder="Local Codex"
        i18n-hint="Hint under the credential name field"
        hint="Describe where this credential will be used."
        maxLength="128" />

      <fieldset class="mt-2">
        <legend class="mb-1 text-sm font-medium">
          <span i18n="Heading above the permission scopes for a credential">
            Credential scopes
          </span>
        </legend>
        <p class="text-muted mb-3 text-xs">
          <span
            i18n="
              Explains that credential scopes narrow the account permissions
            ">
            Scopes can restrict this credential further than the service
            account.
          </span>
        </p>

        <div class="border-border divide-border divide-y rounded border">
          @for (permission of account.permissions; track permission) {
            <div class="px-4 py-3">
              <app-checkbox
                [checked]="hasScope(permission)"
                (changed)="setScope(permission, $event)">
                <span class="text-sm">
                  {{ getPermissionLabel(permission) }}
                </span>
              </app-checkbox>
            </div>
          } @empty {
            <p class="text-muted px-4 py-3 text-sm">
              <span
                i18n="Shown when a service account has no permissions to scope">
                This service account has no API permissions.
              </span>
            </p>
          }
        </div>
      </fieldset>
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">
        <span i18n="Dismisses a dialog without acting">Cancel</span>
      </button>
      <button
        app-flat-button
        type="button"
        [disabled]="selectedScopes().size === 0"
        (click)="submit($event)">
        <span i18n="Button that creates the API credential">
          Create Credential
        </span>
      </button>
    </div>
  `,
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
        label: $localize`:Field name used inside validation messages, e.g. "Credential name is required.":Credential name`,
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
