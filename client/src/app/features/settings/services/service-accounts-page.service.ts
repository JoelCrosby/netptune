import { DestroyRef, Injectable, computed, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  ApiCredential,
  ApiCredentialCreated,
  CreateApiCredentialRequest,
  ServiceAccount,
  UpdateApiCredentialScopesRequest,
  UpdateServiceAccountRequest,
} from '@core/models/service-account';
import { serviceAccountResource } from '@core/resources/service-account.resource';
import { ConfirmationService } from '@core/services/confirmation.service';
import { DialogService } from '@core/services/dialog.service';
import { ServiceAccountsService } from '@core/services/service-accounts.service';
import { mutation } from '@core/util/mutation';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import {
  Observable,
  catchError,
  firstValueFrom,
  map,
  of,
  switchMap,
} from 'rxjs';
import { ApiCredentialSecretDialogComponent } from '../components/service-accounts/api-credential-secret-dialog.component';
import { CreateApiCredentialDialogComponent } from '../components/service-accounts/create-api-credential-dialog.component';
import {
  CreateServiceAccountDialogComponent,
  CreateServiceAccountWizardResult,
} from '../components/service-accounts/create-service-account-dialog.component';
import {
  EditApiCredentialScopesDialogComponent,
  EditApiCredentialScopesDialogData,
} from '../components/service-accounts/edit-api-credential-scopes-dialog.component';
import {
  EditServiceAccountDialogComponent,
  EditServiceAccountDialogData,
} from '../components/service-accounts/edit-service-account-dialog.component';

@Injectable()
export class ServiceAccountsPageService {
  private readonly service = inject(ServiceAccountsService);
  private readonly dialog = inject(DialogService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly destroyRef = inject(DestroyRef);

  readonly accounts = serviceAccountResource();
  private readonly request = mutation();

  readonly busy = this.request.pending;

  readonly canCreate = hasPermission(PERMISSIONS.serviceAccounts.create);
  readonly canUpdate = hasPermission(PERMISSIONS.serviceAccounts.update);
  readonly canDelete = hasPermission(PERMISSIONS.serviceAccounts.delete);
  readonly canManageCredentials = hasPermission(
    PERMISSIONS.serviceAccounts.manageCredentials
  );

  readonly loading = computed(() => {
    return this.accounts.isLoading() && !this.accounts.hasValue();
  });

  readonly sortedAccounts = computed(() => {
    return [...this.accounts.value()].sort((left, right) => {
      return left.name.localeCompare(right.name);
    });
  });

  reload() {
    this.accounts.reload();
  }

  async createAccount() {
    const dialogRef = this.dialog.openWizard<CreateServiceAccountWizardResult>(
      CreateServiceAccountDialogComponent,
      {
        title: $localize`:Title of the create-service-account dialog:Create Service Account`,
        width: '720px',
      }
    );

    const result = await firstValueFrom(dialogRef.closed, {
      defaultValue: undefined,
    });

    if (!result) return;

    this.mutate(
      this.createAccountWithCredential(result),
      ({ credential, credentialFailed }) => {
        if (credentialFailed) {
          this.snackbar.warn(
            'Service account created, but its credential could not be created'
          );
        } else {
          this.snackbar.success(
            credential
              ? 'Service account and credential created'
              : 'Service account created'
          );
        }

        if (credential) {
          this.openCredentialSecret(credential);
        }
      },
      $localize`:Error after failing to create a service account:Service account could not be created`
    );
  }

  async editAccount(account: ServiceAccount) {
    const request = await this.dialog.openForResult<
      UpdateServiceAccountRequest,
      EditServiceAccountDialogData
    >(EditServiceAccountDialogComponent, {
      data: { account },
      width: '720px',
    });

    if (!request) return;

    this.mutate(
      this.service.update(account.id, request),
      () => {
        this.snackbar.success(
          $localize`:Confirmation after updating a service account:Service account updated`
        );
      },
      $localize`:Error after failing to update a service account:Service account could not be updated`
    );
  }

  async deleteAccount(account: ServiceAccount) {
    const accepted = await firstValueFrom(
      this.confirmation.open({
        title: $localize`:Title of the confirmation dialog for deleting a service account:Delete Service Account`,
        message: $localize`:Confirmation body for deleting a service account. NAME is the account name:Delete "${account.name}:NAME:"? All of its credentials will immediately stop working. The account will remain in history as disabled.`,
        acceptLabel: $localize`:Confirms a destructive action:Delete`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
    );

    if (!accepted) return;

    this.mutate(
      this.service.delete(account.id),
      () => {
        this.snackbar.success(
          $localize`:Confirmation after deleting a service account:Service account deleted`
        );
      },
      $localize`:Error after failing to delete a service account:Service account could not be deleted`
    );
  }

  async createCredential(account: ServiceAccount) {
    const request = await this.dialog.openForResult<
      CreateApiCredentialRequest,
      ServiceAccount
    >(CreateApiCredentialDialogComponent, {
      data: account,
      width: '560px',
    });

    if (!request) return;

    this.mutate(
      this.service.createCredential(account.id, request),
      (credential) => {
        this.openCredentialSecret(credential);
        this.snackbar.success(
          $localize`:Confirmation after creating an API credential:API credential created`
        );
      },
      $localize`:Error after failing to create a credential:Credential could not be created`
    );
  }

  async editCredentialScopes(
    account: ServiceAccount,
    credential: ApiCredential
  ) {
    const request = await this.dialog.openForResult<
      UpdateApiCredentialScopesRequest,
      EditApiCredentialScopesDialogData
    >(EditApiCredentialScopesDialogComponent, {
      data: { account, credential },
      width: '600px',
    });

    if (!request) return;

    this.mutate(
      this.service.updateCredentialScopes(account.id, credential.id, request),
      () => {
        this.snackbar.success(
          $localize`:Confirmation after saving a credential's scopes:Credential scopes updated`
        );
      },
      $localize`:Error after failing to save a credential's scopes:Credential scopes could not be updated`
    );
  }

  async revokeCredential(account: ServiceAccount, credential: ApiCredential) {
    const accepted = await firstValueFrom(
      this.confirmation.open({
        title: $localize`:Title of the confirmation dialog for revoking a credential:Revoke API Credential`,
        message: $localize`:Confirmation body for revoking a credential. NAME is the credential name:Revoke "${credential.name}:NAME:"? Any agent using it will immediately lose access.`,
        acceptLabel: $localize`:Confirms revoking a credential:Revoke`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
    );

    if (!accepted) return;

    this.mutate(
      this.service.revokeCredential(account.id, credential.id),
      () => {
        this.snackbar.success(
          $localize`:Confirmation after revoking a credential:Credential revoked`
        );
      },
      $localize`:Error after failing to revoke a credential:Credential could not be revoked`
    );
  }

  private createAccountWithCredential(
    result: CreateServiceAccountWizardResult
  ) {
    return this.service.create(result.account).pipe(
      switchMap((account) => {
        if (!result.credential) {
          return of({ credential: undefined, credentialFailed: false });
        }

        return this.service
          .createCredential(account.id, result.credential)
          .pipe(
            map((credential) => ({ credential, credentialFailed: false })),
            catchError(() => {
              return of({ credential: undefined, credentialFailed: true });
            })
          );
      })
    );
  }

  private mutate<T>(
    request: Observable<T>,
    onSuccess: (result: T) => void,
    errorMessage: string
  ) {
    this.request.run(request.pipe(takeUntilDestroyed(this.destroyRef)), {
      onSuccess: (result) => {
        onSuccess(result);
        this.reload();
      },
      onError: () => this.snackbar.error(errorMessage),
    });
  }

  private openCredentialSecret(credential: ApiCredentialCreated) {
    this.dialog.open<unknown, ApiCredentialCreated>(
      ApiCredentialSecretDialogComponent,
      {
        data: credential,
        width: '640px',
        disableClose: true,
      }
    );
  }
}
