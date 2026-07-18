import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import {
  FormField,
  form,
  maxLength,
  minLength,
  required,
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
import { permissionLabel } from './service-account-permissions';

interface ApiPermissionOption {
  key: Permission;
  label: string;
  description: string;
}

export interface CreateServiceAccountWizardResult {
  account: CreateServiceAccountRequest;
  credential?: CreateApiCredentialRequest;
}

const apiPermissionOptions: ApiPermissionOption[] = [
  {
    key: netptunePermissions.projects.read,
    label: 'View projects',
    description: 'Find project IDs and project metadata.',
  },
  {
    key: netptunePermissions.statuses.read,
    label: 'View statuses',
    description: 'Resolve workflow status IDs.',
  },
  {
    key: netptunePermissions.sprints.read,
    label: 'View sprints',
    description: 'Read sprint details and current scope.',
  },
  {
    key: netptunePermissions.sprints.create,
    label: 'Create sprints',
    description: 'Create planning sprints for projects.',
  },
  {
    key: netptunePermissions.sprints.update,
    label: 'Update sprints',
    description: 'Edit sprint details and lifecycle state.',
  },
  {
    key: netptunePermissions.sprints.delete,
    label: 'Delete sprints',
    description: 'Delete planning or cancelled sprints.',
  },
  {
    key: netptunePermissions.sprints.manageTasks,
    label: 'Manage sprint tasks',
    description: 'Add tasks to and remove tasks from sprints.',
  },
  {
    key: netptunePermissions.tasks.read,
    label: 'View tasks',
    description: 'Read tasks and their current state.',
  },
  {
    key: netptunePermissions.tasks.create,
    label: 'Create tasks',
    description: 'Create new workspace tasks.',
  },
  {
    key: netptunePermissions.tasks.update,
    label: 'Update tasks',
    description: 'Change task fields and progress.',
  },
];

@Component({
  selector: 'app-create-service-account-dialog',
  imports: [
    FormField,
    FormInputComponent,
    FormTextAreaComponent,
    CheckboxComponent,
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
  template: `<form app-dialog-content class="min-w-0">
      <app-stepper mode="wizard" [(activeIndex)]="currentStep">
        <app-step
          title="Account details"
          description="Name and describe the service account.">
          <div class="form-auth">
            <p class="text-muted mb-5 text-sm">
              Service accounts are non-interactive identities owned by workspace
              users. They cannot sign in through the normal login flow.
            </p>

            <app-form-input
              [formField]="accountForm.name"
              label="Name"
              placeholder="Codex"
              hint="Use a name that identifies the agent or integration."
              maxLength="128" />

            <app-form-textarea
              [formField]="accountForm.description"
              label="Description"
              placeholder="Tracks implementation work in Netptune"
              rows="3"
              maxLength="2048" />
          </div>
        </app-step>

        <app-step
          title="Permissions"
          description="Choose what this identity is allowed to do.">
          <fieldset>
            <legend class="mb-1 text-sm font-medium">API permissions</legend>
            <p class="text-muted mb-3 text-xs">
              Every credential is restricted to a subset of these account
              permissions.
            </p>

            <div class="border-border divide-border divide-y rounded border">
              @for (permission of permissionOptions; track permission.key) {
                <div class="px-4 py-3">
                  <app-checkbox
                    [checked]="hasPermission(permission.key)"
                    (changed)="setPermission(permission.key, $event)">
                    <span class="flex flex-col gap-0.5">
                      <span class="text-sm font-medium">
                        {{ permission.label }}
                      </span>
                      <span class="text-muted text-xs">
                        {{ permission.description }}
                      </span>
                    </span>
                  </app-checkbox>
                </div>
              }
            </div>

            @if (selectedPermissions().size === 0) {
              <p class="text-warn mt-2 text-sm">
                Select at least one permission to continue.
              </p>
            }
          </fieldset>
        </app-step>

        <app-step
          title="First credential"
          description="Optionally issue the first API credential.">
          <div class="border-border mb-5 rounded border px-4 py-3">
            <app-checkbox
              [checked]="createCredential()"
              (changed)="setCreateCredential($event)">
              <span class="flex flex-col gap-0.5">
                <span class="text-sm font-medium">Create a credential now</span>
                <span class="text-muted text-xs">
                  You can skip this and create one from the service-account page
                  later.
                </span>
              </span>
            </app-checkbox>
          </div>

          @if (createCredential()) {
            <app-form-input
              [formField]="accountForm.credentialName"
              label="Credential name"
              placeholder="Local Codex"
              hint="Describe where this credential will be used."
              maxLength="128" />

            <fieldset>
              <legend class="mb-1 text-sm font-medium">
                Credential scopes
              </legend>
              <p class="text-muted mb-3 text-xs">
                Restrict this credential further than the account if needed.
              </p>

              <div class="border-border divide-border divide-y rounded border">
                @for (
                  permission of selectedPermissionOptions();
                  track permission.key
                ) {
                  <div class="px-4 py-3">
                    <app-checkbox
                      [checked]="hasCredentialScope(permission.key)"
                      (changed)="setCredentialScope(permission.key, $event)">
                      <span class="text-sm">{{ permission.label }}</span>
                    </app-checkbox>
                  </div>
                }
              </div>

              @if (credentialScopes().size === 0) {
                <p class="text-warn mt-2 text-sm">
                  Select at least one credential scope or skip credential
                  creation.
                </p>
              }
            </fieldset>
          } @else {
            <div
              class="border-border bg-background flex min-h-40 flex-col items-center justify-center rounded border p-6 text-center">
              <svg lucideKeyRound class="text-muted mb-3 h-8 w-8"></svg>
              <p class="font-medium">No credential will be created</p>
              <p class="text-muted mt-1 text-sm">
                The service account will be ready for a credential whenever you
                need one.
              </p>
            </div>
          }
        </app-step>

        <app-step title="Review" description="Review what will be created.">
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
                Account permissions
              </h4>
              <div class="flex flex-wrap gap-2">
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
                First credential
              </h4>
              @if (createCredential()) {
                <p class="text-sm font-medium">
                  {{ accountForm.credentialName().value() }}
                </p>
                <p class="text-muted mt-1 text-xs">
                  {{ credentialScopes().size }} scoped permissions · expires
                  after 90 days
                </p>
              } @else {
                <p class="text-muted text-sm">
                  Credential creation will be skipped.
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
          Back
        </button>
      }

      @if (currentStep() < finalStep) {
        <button
          app-flat-button
          class="ml-auto"
          type="button"
          (click)="nextStep()">
          Next
          <svg lucideChevronRight class="h-4 w-4" aria-hidden="true"></svg>
        </button>
      } @else {
        <button
          app-flat-button
          class="ml-auto"
          type="button"
          (click)="submit()">
          Create Service Account
        </button>
      }
    </div>`,
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
  readonly permissionOptions = apiPermissionOptions;
  readonly selectedPermissions = signal<Set<Permission>>(
    new Set(apiPermissionOptions.map((permission) => permission.key))
  );
  readonly credentialScopes = signal<Set<Permission>>(
    new Set(apiPermissionOptions.map((permission) => permission.key))
  );

  readonly accountFormModel = signal({
    name: '',
    description: '',
    createCredential: true,
    credentialName: 'Default credential',
  });

  readonly accountForm = form(this.accountFormModel, (schema) => {
    required(schema.name, { message: 'Name is required.' });
    minLength(schema.name, 2, {
      message: 'Name must have at least 2 characters.',
    });
    maxLength(schema.name, 128);
    maxLength(schema.description, 2048);
    required(schema.credentialName, {
      message: 'Credential name is required.',
      when: (context) => context.valueOf(schema.createCredential),
    });
    minLength(schema.credentialName, 2);
    maxLength(schema.credentialName, 128);
  });

  readonly createCredential = computed(() =>
    this.accountForm.createCredential().value()
  );
  readonly selectedPermissionOptions = computed(() => {
    const permissions = this.selectedPermissions();
    return this.permissionOptions.filter((option) =>
      permissions.has(option.key)
    );
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

  hasCredentialScope(permission: Permission) {
    return this.credentialScopes().has(permission);
  }

  setCredentialScope(permission: Permission, selected: boolean) {
    this.credentialScopes.update((current) =>
      this.updateSelection(current, permission, selected)
    );
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
