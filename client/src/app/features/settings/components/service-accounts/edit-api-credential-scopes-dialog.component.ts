import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { Permission } from '@core/auth/permissions';
import {
  ApiCredential,
  ServiceAccount,
  UpdateApiCredentialScopesRequest,
} from '@core/models/service-account';
import { toggleInSet } from '@core/util/signals';
import { LucideTriangleAlert } from '@lucide/angular';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { PermissionGridComponent } from './permission-grid.component';
import {
  filterPermissionGroups,
  permissionLabel,
} from './service-account-permissions';

export interface EditApiCredentialScopesDialogData {
  account: ServiceAccount;
  credential: ApiCredential;
}

@Component({
  selector: 'app-edit-api-credential-scopes-dialog',
  imports: [
    DialogActionsDirective,
    DialogCloseDirective,
    DialogTitleComponent,
    FlatButtonComponent,
    CalloutComponent,
    InlineButtonComponent,
    PermissionGridComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title i18n="Title of the edit-credential-scopes dialog">
      Edit Credential Scopes
    </app-dialog-title>

    <div app-dialog-content class="flex flex-col gap-4">
      <p class="text-muted text-sm">
        <span
          i18n="
            Explains that credential scopes are fixed. NAME is the credential
            name
          ">
          <strong>{{ credential.name }}</strong> can only use the permissions
          selected here. Permissions added to the service account later are not
          added to it automatically. The secret does not change.
        </span>
      </p>

      @if (missingScopes().length > 0) {
        <app-callout color="warn" [icon]="warningIcon">
          <div class="flex flex-col gap-1">
            <span
              i18n="
                Warns that the credential is missing some of the account's
                permissions. COUNT is how many
              ">
              {missingScopes().length, plural,
                =1 {This credential lacks 1 permission the account has:}
                other {
                  This credential lacks {{ missingScopes().length }} permissions
                  the account has:
                }
              }
            </span>
            <span class="text-muted text-xs">{{ missingLabels() }}</span>
            <button
              app-inline-button
              class="self-start text-[13px]"
              (click)="selectAll()">
              <span
                i18n="
                  Button that adds every account permission to a credential
                ">
                Grant all account permissions
              </span>
            </button>
          </div>
        </app-callout>
      }

      <app-permission-grid
        maxHeightClass="max-h-80"
        [groups]="scopeGroups"
        [selected]="selectedScopes()"
        [emptyMessage]="noPermissionsMessage"
        (permissionChanged)="setScope($event.permission, $event.selected)"
        (selectAllRequested)="selectAll()"
        (clearRequested)="clear()" />
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">
        <span i18n="Dismisses a dialog without acting">Cancel</span>
      </button>
      <button
        app-flat-button
        type="button"
        [disabled]="selectedScopes().size === 0"
        (click)="submit()">
        <span i18n="Button that saves a credential's scopes">Save Scopes</span>
      </button>
    </div>
  `,
})
export class EditApiCredentialScopesDialogComponent {
  private readonly dialogRef =
    inject<
      DialogRef<
        UpdateApiCredentialScopesRequest,
        EditApiCredentialScopesDialogComponent
      >
    >(DialogRef);

  private readonly data =
    inject<EditApiCredentialScopesDialogData>(DIALOG_DATA);

  protected readonly warningIcon = LucideTriangleAlert;

  readonly account = this.data.account;
  readonly credential = this.data.credential;
  readonly scopeGroups = filterPermissionGroups(this.account.permissions);

  readonly selectedScopes = signal<ReadonlySet<Permission>>(
    new Set(this.credential.scopes)
  );

  readonly missingScopes = computed(() => {
    const selected = this.selectedScopes();

    return this.account.permissions.filter(
      (permission) => !selected.has(permission)
    );
  });

  readonly missingLabels = computed(() => {
    return this.missingScopes().map(permissionLabel).join(', ');
  });

  readonly noPermissionsMessage = $localize`:Shown when a service account has no permissions to scope:This service account has no API permissions.`;

  setScope(permission: Permission, selected: boolean) {
    toggleInSet(this.selectedScopes, permission, selected);
  }

  selectAll() {
    this.selectedScopes.set(new Set(this.account.permissions));
  }

  clear() {
    this.selectedScopes.set(new Set());
  }

  submit() {
    if (this.selectedScopes().size === 0) return;

    this.dialogRef.close({ scopes: [...this.selectedScopes()] });
  }
}
