import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import {
  apply,
  FormField,
  form,
  maxLength,
  submit,
} from '@angular/forms/signals';
import { Permission } from '@core/auth/permissions';
import {
  ServiceAccount,
  UpdateServiceAccountRequest,
} from '@core/models/service-account';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import {
  allPermissions,
  permissionGroups,
  PermissionGroupOption,
} from './service-account-permissions';

export interface EditServiceAccountDialogData {
  account: ServiceAccount;
}

@Component({
  selector: 'app-edit-service-account-dialog',
  imports: [
    CheckboxComponent,
    DialogActionsDirective,
    DialogTitleComponent,
    FlatButtonComponent,
    FormField,
    FormInputComponent,
    FormTextAreaComponent,
    StrokedButtonComponent,
  ],
  template: `<app-dialog-title>Edit Service Account</app-dialog-title>

    <form class="flex min-w-0 flex-col gap-3" (submit)="onSubmit($event)">
      <app-form-input
        [formField]="accountForm.name"
        label="Name"
        maxLength="128" />

      <app-form-textarea
        [formField]="accountForm.description"
        label="Description"
        rows="3"
        maxLength="2048" />

      <fieldset>
        <legend class="mb-1 text-sm font-medium">API permissions</legend>
        <p class="text-muted mb-3 text-xs">
          Removing a permission also removes it from every credential on this
          account.
        </p>

        <div class="mb-3 flex items-center justify-between gap-3">
          <span class="text-muted text-xs">
            {{ selectedPermissions().size }} of {{ totalPermissionCount }}
            selected
          </span>
          <div class="flex gap-2">
            <button
              app-stroked-button
              type="button"
              class="h-8 text-xs"
              (click)="selectAllPermissions()">
              Select all
            </button>
            <button
              app-stroked-button
              type="button"
              class="h-8 text-xs"
              (click)="clearPermissions()">
              Clear
            </button>
          </div>
        </div>

        <div
          class="border-border divide-border max-h-96 divide-y overflow-y-auto rounded border">
          @for (group of permissionGroups; track group.key) {
            <section>
              <header
                class="bg-foreground/3 flex items-center justify-between gap-2 px-4 py-2">
                <h4 class="text-xs font-semibold tracking-wide uppercase">
                  {{ group.label }}
                </h4>
                <button
                  type="button"
                  class="text-primary cursor-pointer text-xs"
                  (click)="toggleGroup(group)">
                  {{ isGroupSelected(group) ? 'Clear group' : 'Select group' }}
                </button>
              </header>

              <div
                class="divide-border grid divide-y sm:grid-cols-2 sm:divide-y-0">
                @for (permission of group.permissions; track permission.key) {
                  <div class="px-4 py-2">
                    <app-checkbox
                      [checked]="hasPermission(permission.key)"
                      (changed)="setPermission(permission.key, $event)">
                      <span class="text-sm">{{ permission.label }}</span>
                    </app-checkbox>
                  </div>
                }
              </div>
            </section>
          }
        </div>

        @if (selectedPermissions().size === 0) {
          <p class="text-warn mt-2 text-sm">
            Select at least one permission to continue.
          </p>
        }
      </fieldset>
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="close()">Cancel</button>
      <button
        app-flat-button
        color="primary"
        type="button"
        [disabled]="!canSave()"
        (click)="onSubmit($event)">
        Save Changes
      </button>
    </div>`,
})
export class EditServiceAccountDialogComponent {
  private readonly dialogRef =
    inject<
      DialogRef<UpdateServiceAccountRequest, EditServiceAccountDialogComponent>
    >(DialogRef);

  readonly dialogData = inject<EditServiceAccountDialogData>(DIALOG_DATA);

  readonly permissionGroups = permissionGroups;
  readonly totalPermissionCount = allPermissions.length;

  readonly selectedPermissions = signal<Set<Permission>>(
    new Set(this.dialogData.account.permissions)
  );

  readonly accountFormModel = signal({
    name: this.dialogData.account.name,
    description: this.dialogData.account.description ?? '',
  });

  readonly accountForm = form(this.accountFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({ label: 'Name', minLength: 2, maxLength: 128 })
    );
    maxLength(schema.description, 2048);
  });

  readonly canSave = computed(() => this.selectedPermissions().size > 0);

  hasPermission(permission: Permission) {
    return this.selectedPermissions().has(permission);
  }

  setPermission(permission: Permission, selected: boolean) {
    this.selectedPermissions.update((current) => {
      const next = new Set(current);

      if (selected) {
        next.add(permission);
      } else {
        next.delete(permission);
      }

      return next;
    });
  }

  isGroupSelected(group: PermissionGroupOption) {
    const selected = this.selectedPermissions();

    return group.permissions.every((permission) =>
      selected.has(permission.key)
    );
  }

  toggleGroup(group: PermissionGroupOption) {
    const select = !this.isGroupSelected(group);

    for (const permission of group.permissions) {
      this.setPermission(permission.key, select);
    }
  }

  selectAllPermissions() {
    this.selectedPermissions.set(new Set(allPermissions));
  }

  clearPermissions() {
    this.selectedPermissions.set(new Set());
  }

  close() {
    this.dialogRef.close();
  }

  onSubmit(event: Event) {
    event.preventDefault();

    if (!this.canSave()) return;

    submit(this.accountForm, async () => {
      const description = this.accountForm.description().value().trim();

      this.dialogRef.close({
        name: this.accountForm.name().value().trim(),
        description: description || undefined,
        permissions: [...this.selectedPermissions()],
        ownerUserIds: this.dialogData.account.ownerUserIds,
      });
    });
  }
}
