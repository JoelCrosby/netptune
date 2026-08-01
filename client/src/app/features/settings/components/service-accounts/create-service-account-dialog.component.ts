import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import {
  apply,
  FormField,
  form,
  maxLength,
  required,
  submit as submitForm,
  validate,
} from '@angular/forms/signals';
import { Permission, netptunePermissions } from '@core/auth/permissions';
import {
  CreateApiCredentialRequest,
  CreateServiceAccountRequest,
} from '@core/models/service-account';
import {
  LucideChevronLeft,
  LucideChevronRight,
  LucideKeyRound,
  LucideShieldCheck,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { StepComponent } from '@static/components/stepper/step.component';
import { StepperComponent } from '@static/components/stepper/stepper.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { PermissionGridComponent } from './permission-grid.component';
import {
  allPermissions,
  filterPermissionGroups,
  permissionGroups,
  permissionLabel,
} from './service-account-permissions';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

export interface CreateServiceAccountWizardResult {
  account: CreateServiceAccountRequest;
  credential?: CreateApiCredentialRequest;
}

const defaultPermissions: Permission[] = [
  netptunePermissions.projects.read,
  netptunePermissions.statuses.read,
  netptunePermissions.tasks.read,
  netptunePermissions.tasks.create,
  netptunePermissions.tasks.update,
];

@Component({
  selector: 'app-create-service-account-dialog',
  imports: [
    FormField,
    FormInputComponent,
    FormTextAreaComponent,
    CheckboxComponent,
    PermissionGridComponent,
    StepperComponent,
    StepComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    LucideChevronLeft,
    LucideChevronRight,
    LucideKeyRound,
    LucideShieldCheck,
  ],
  template: `
    <form app-dialog-content class="min-w-0">
      <app-stepper mode="wizard" [(activeIndex)]="currentStep">
        <app-step
          i18n-title="Title of the account details wizard step"
          title="Account details"
          i18n-description="Description of the account details wizard step"
          description="Name and describe the service account.">
          <div class="form-auth">
            <p class="text-muted mb-5 text-sm">
              <span i18n="Explains what a service account is">
                Service accounts are non-interactive identities owned by
                workspace users. They cannot sign in through the normal login
                flow.
              </span>
            </p>

            <app-form-input
              [formField]="accountForm.name"
              i18n-label="Label of the name field"
              label="Name"
              i18n-placeholder="
                Example service account name shown as placeholder text
              "
              placeholder="Codex"
              i18n-hint="Hint under the service account name field"
              hint="Use a name that identifies the agent or integration."
              maxLength="128" />

            <app-form-textarea
              [formField]="accountForm.description"
              i18n-label="Label of the description field"
              label="Description"
              i18n-placeholder="
                Example service account description shown as placeholder text
              "
              placeholder="Tracks implementation work in Netptune"
              rows="3"
              maxLength="2048" />
          </div>
        </app-step>

        <app-step
          i18n-title="Title of the permissions wizard step"
          title="Permissions"
          i18n-description="Description of the permissions wizard step"
          description="Choose what this identity is allowed to do.">
          <fieldset>
            <legend class="mb-1 text-sm font-medium">
              <span
                i18n="Heading above the API permissions for a service account">
                API permissions
              </span>
            </legend>
            <p class="text-muted mb-3 text-xs">
              <span
                i18n="
                  Explains that credential scopes are a subset of account
                  permissions
                ">
                Every credential is restricted to a subset of these account
                permissions.
              </span>
            </p>

            <app-permission-grid
              [groups]="permissionGroups"
              [selected]="selectedPermissions()"
              (permissionChanged)="
                setPermission($event.permission, $event.selected)
              "
              (selectAllRequested)="selectAllPermissions()"
              (clearRequested)="clearPermissions()" />

            @if (selectedPermissions().size === 0) {
              <p class="text-warn mt-2 text-sm">
                <span i18n="Validation message when no permission is selected">
                  Select at least one permission to continue.
                </span>
              </p>
            }
          </fieldset>
        </app-step>

        <app-step
          i18n-title="Title of the first-credential wizard step"
          title="First credential"
          i18n-description="Description of the first-credential wizard step"
          description="Optionally issue the first API credential.">
          <div class="border-border mb-5 rounded border px-4 py-3">
            <app-checkbox
              [checked]="createCredential()"
              (changed)="setCreateCredential($event)">
              <span class="flex flex-col gap-0.5">
                <span
                  class="text-sm font-medium"
                  i18n="
                    Option to issue a credential while creating the account
                  ">
                  Create a credential now
                </span>
                <span class="text-muted text-xs">
                  <span
                    i18n="Explains that issuing a credential can be deferred">
                    You can skip this and create one from the service-account
                    page later.
                  </span>
                </span>
              </span>
            </app-checkbox>
          </div>

          @if (createCredential()) {
            <app-form-input
              [formField]="accountForm.credentialName"
              i18n-label="Label of the credential name field"
              label="Credential name"
              i18n-placeholder="
                Example credential name shown as placeholder text
              "
              placeholder="Local Codex"
              i18n-hint="Hint under the credential name field"
              hint="Describe where this credential will be used."
              maxLength="128" />

            <fieldset>
              <legend class="mb-1 text-sm font-medium">
                <span
                  i18n="Heading above the permission scopes for a credential">
                  Credential scopes
                </span>
              </legend>
              <p class="text-muted mb-3 text-xs">
                <span
                  i18n="
                    Explains that a credential can be narrower than the account
                  ">
                  Restrict this credential further than the account if needed.
                </span>
              </p>

              <app-permission-grid
                maxHeightClass="max-h-72"
                [groups]="scopeGroups()"
                [selected]="credentialScopes()"
                (permissionChanged)="
                  setCredentialScope($event.permission, $event.selected)
                "
                (selectAllRequested)="selectAllCredentialScopes()"
                (clearRequested)="clearCredentialScopes()" />

              @if (credentialScopes().size === 0) {
                <p class="text-warn mt-2 text-sm">
                  <span
                    i18n="
                      Validation message when a credential has no scopes
                      selected
                    ">
                    Select at least one credential scope or skip credential
                    creation.
                  </span>
                </p>
              }
            </fieldset>
          } @else {
            <div
              class="border-border bg-background flex min-h-40 flex-col items-center justify-center rounded border p-6 text-center">
              <svg lucideKeyRound class="text-muted mb-3 h-8 w-8"></svg>
              <p class="font-medium">
                <span
                  i18n="
                    Shown when the user opts out of issuing a first credential
                  ">
                  No credential will be created
                </span>
              </p>
              <p class="text-muted mt-1 text-sm">
                <span i18n="Reassures that a credential can be issued later">
                  The service account will be ready for a credential whenever
                  you need one.
                </span>
              </p>
            </div>
          }
        </app-step>

        <app-step
          i18n-title="Title of the review wizard step"
          title="Review"
          i18n-description="Description of the review wizard step"
          description="Review what will be created.">
          <div class="flex flex-col gap-4">
            <div class="border-border rounded border p-4">
              <h3 class="font-overpass text-lg font-medium">
                {{ accountForm.name().value() }}
              </h3>
              @if (accountForm.description().value()) {
                <p class="text-muted mt-1 text-sm">
                  {{ accountForm.description().value() }}
                </p>
              }
            </div>

            <div class="border-border rounded border p-4">
              <h4 class="mb-3 flex items-center gap-2 text-sm font-medium">
                <svg lucideShieldCheck class="h-4 w-4"></svg>
                <span
                  i18n="
                    Heading above the permissions granted to a service account
                  ">
                  Account permissions
                </span>
              </h4>
              <div class="flex max-h-40 flex-wrap gap-2 overflow-y-auto">
                @for (permission of selectedPermissions(); track permission) {
                  <span
                    class="bg-foreground/10 text-foreground rounded px-2 py-1 text-xs">
                    {{ getPermissionLabel(permission) }}
                  </span>
                }
              </div>
            </div>

            <div class="border-border rounded border p-4">
              <h4 class="mb-2 flex items-center gap-2 text-sm font-medium">
                <svg lucideKeyRound class="h-4 w-4"></svg>
                <span
                  i18n="Heading above the first credential in the review step">
                  First credential
                </span>
              </h4>
              @if (createCredential()) {
                <p class="text-sm font-medium">
                  {{ accountForm.credentialName().value() }}
                </p>
                <p class="text-muted mt-1 text-xs">
                  <span
                    i18n="
                      Credential summary in the review step. COUNT is the number
                      of scoped permissions; the 90 day expiry is fixed by the
                      server
                    ">
                    {{
                      credentialScopes().size // i18n(ph="COUNT")
                    }}
                    scoped permissions · expires after 90 days
                  </span>
                </p>
              } @else {
                <p class="text-muted text-sm">
                  <span
                    i18n="
                      Shown in the review step when no credential will be issued
                    ">
                    Credential creation will be skipped.
                  </span>
                </p>
              }
            </div>
          </div>
        </app-step>
      </app-stepper>
    </form>

    <div app-dialog-actions>
      @if (currentStep() > 0) {
        <button app-stroked-button type="button" (click)="previousStep()">
          <svg lucideChevronLeft class="h-4 w-4" aria-hidden="true"></svg>
          <span i18n="Button that returns to the previous wizard step">
            Back
          </span>
        </button>
      }

      @if (currentStep() < finalStep) {
        <button
          app-flat-button
          class="ml-auto"
          type="button"
          (click)="nextStep()">
          <span i18n="Button that advances to the next wizard step">Next</span>
          <svg lucideChevronRight class="h-4 w-4" aria-hidden="true"></svg>
        </button>
      } @else {
        <button
          app-flat-button
          class="ml-auto"
          type="button"
          (click)="submit()">
          <span i18n="Button that creates the service account">
            Create Service Account
          </span>
        </button>
      }
    </div>
  `,
})
export class CreateServiceAccountDialogComponent {
  private readonly dialogRef =
    inject<
      DialogRef<
        CreateServiceAccountWizardResult,
        CreateServiceAccountDialogComponent
      >
    >(DialogRef);

  readonly currentStep = signal(0);
  readonly finalStep = 3;
  readonly permissionGroups = permissionGroups;
  readonly totalPermissionCount = allPermissions.length;
  readonly selectedPermissions = signal<Set<Permission>>(
    new Set(defaultPermissions)
  );
  readonly credentialScopes = signal<Set<Permission>>(
    new Set(defaultPermissions)
  );

  readonly accountFormModel = signal({
    name: '',
    description: '',
    createCredential: true,
    credentialName: 'Default credential',
  });

  readonly accountForm = form(this.accountFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Name is required.":Name`,
        minLength: 2,
        maxLength: 128,
      })
    );
    maxLength(schema.description, 2048);
    required(schema.credentialName, {
      message: $localize`:Validation error when the credential name is empty:Credential name is required.`,
      when: (context) => context.valueOf(schema.createCredential),
    });
    validate(schema.credentialName, (context) => {
      if (!context.valueOf(schema.createCredential)) return undefined;

      const name = context.value().trim();

      if (!name) {
        return {
          kind: 'whitespace',
          message: $localize`:Validation error when the credential name is empty:Credential name is required.`,
        };
      }

      if (name.length >= 2) return undefined;

      return {
        kind: 'minLength',
        message: $localize`:Validation error when the credential name is too short:Credential name must have at least 2 characters.`,
      };
    });
    maxLength(schema.credentialName, 128);
  });

  readonly createCredential = computed(() =>
    this.accountForm.createCredential().value()
  );
  readonly scopeGroups = computed(() => {
    return filterPermissionGroups(this.selectedPermissions());
  });
  readonly selectedPermissionOptions = computed(() => {
    const permissions = this.selectedPermissions();

    return permissionGroups
      .flatMap((group) => group.permissions)
      .filter((option) => permissions.has(option.key));
  });

  hasPermission(permission: Permission) {
    return this.selectedPermissions().has(permission);
  }

  setPermission(permission: Permission, selected: boolean) {
    this.selectedPermissions.update((current) =>
      this.updateSelection(current, permission, selected)
    );
    this.credentialScopes.update((current) =>
      this.updateSelection(current, permission, selected)
    );
  }

  selectAllPermissions() {
    for (const permission of allPermissions) {
      this.setPermission(permission, true);
    }
  }

  clearPermissions() {
    this.selectedPermissions.set(new Set());
    this.credentialScopes.set(new Set());
  }

  hasCredentialScope(permission: Permission) {
    return this.credentialScopes().has(permission);
  }

  setCredentialScope(permission: Permission, selected: boolean) {
    this.credentialScopes.update((current) =>
      this.updateSelection(current, permission, selected)
    );
  }

  selectAllCredentialScopes() {
    this.credentialScopes.set(new Set(this.selectedPermissions()));
  }

  clearCredentialScopes() {
    this.credentialScopes.set(new Set());
  }

  setCreateCredential(selected: boolean) {
    this.accountFormModel.update((model) => ({
      ...model,
      createCredential: selected,
    }));
  }

  nextStep() {
    if (!this.isCurrentStepValid()) return;
    this.currentStep.update((step) => Math.min(step + 1, this.finalStep));
  }

  previousStep() {
    this.currentStep.update((step) => Math.max(step - 1, 0));
  }

  submit() {
    if (!this.isComplete()) return;

    submitForm(this.accountForm, async () => {
      const name = this.accountForm.name().value().trim();
      const description = this.accountForm.description().value().trim();
      const createCredential = this.createCredential();

      this.dialogRef.close({
        account: {
          name,
          description: description || undefined,
          permissions: [...this.selectedPermissions()],
          ownerUserIds: [],
        },
        credential: createCredential
          ? {
              name: this.accountForm.credentialName().value().trim(),
              scopes: [...this.credentialScopes()],
            }
          : undefined,
      });
    });
  }

  getPermissionLabel(permission: Permission) {
    return permissionLabel(permission);
  }

  private isCurrentStepValid() {
    if (this.currentStep() === 0) {
      const valid =
        this.accountForm.name().valid() &&
        this.accountForm.description().valid();
      if (!valid) this.accountForm().markAsTouched();
      return valid;
    }

    if (this.currentStep() === 1) {
      return this.selectedPermissions().size > 0;
    }

    if (this.currentStep() === 2 && this.createCredential()) {
      const valid =
        this.accountForm.credentialName().valid() &&
        this.credentialScopes().size > 0;
      if (!valid) this.accountForm.credentialName().markAsTouched();
      return valid;
    }

    return true;
  }

  private isComplete() {
    const valid =
      this.accountForm.name().valid() &&
      this.accountForm.description().valid() &&
      this.selectedPermissions().size > 0 &&
      (!this.createCredential() ||
        (this.accountForm.credentialName().valid() &&
          this.credentialScopes().size > 0));

    if (!valid) this.accountForm().markAsTouched();
    return valid;
  }

  private updateSelection(
    current: Set<Permission>,
    permission: Permission,
    selected: boolean
  ) {
    const next = new Set(current);
    if (selected) {
      next.add(permission);
    } else {
      next.delete(permission);
    }
    return next;
  }
}
