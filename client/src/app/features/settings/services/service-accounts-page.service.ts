import {
  DestroyRef,
  Injectable,
  computed,
  inject,
  signal,
} from '@angular/core';
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
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import {
  EMPTY,
  Observable,
  catchError,
  finalize,
  first,
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

interface MutationMessages<T> {
  success: string | ((result: T) => string);
  error: string;
}

@Injectable()
export class ServiceAccountsPageService {
  private readonly service = inject(ServiceAccountsService);
  private readonly dialog = inject(DialogService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly destroyRef = inject(DestroyRef);

  readonly accounts = serviceAccountResource();
  readonly busy = signal(false);

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

  createAccount() {
    const dialogRef = this.dialog.openWizard<CreateServiceAccountWizardResult>(
      CreateServiceAccountDialogComponent,
      {
        title: $localize`:Title of the create-service-account dialog:Create Service Account`,
        width: '720px',
      }
    );

    const creation = dialogRef.closed.pipe(
      first(),
      switchMap((result) => {
        if (!result) return EMPTY;

        return this.whileBusy(this.createAccountWithCredential(result));
      })
    );

    this.run(
      creation,
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

  editAccount(account: ServiceAccount) {
    const dialogRef = this.dialog.open<
      UpdateServiceAccountRequest,
      EditServiceAccountDialogData
    >(EditServiceAccountDialogComponent, {
      data: { account },
      width: '720px',
    });

    this.mutateOnClose(
      dialogRef.closed,
      (request) => this.service.update(account.id, request),
      {
        success: $localize`:Confirmation after updating a service account:Service account updated`,
        error: $localize`:Error after failing to update a service account:Service account could not be updated`,
      }
    );
  }

  deleteAccount(account: ServiceAccount) {
    const confirmed = this.confirmation.open({
      title: $localize`:Title of the confirmation dialog for deleting a service account:Delete Service Account`,
      message: $localize`:Confirmation body for deleting a service account. NAME is the account name:Delete "${account.name}:NAME:"? All of its credentials will immediately stop working. The account will remain in history as disabled.`,
      acceptLabel: $localize`:Confirms a destructive action:Delete`,
      cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
      color: 'warn',
    });

    this.mutateOnClose(
      confirmed.pipe(map((accepted) => accepted || undefined)),
      () => this.service.delete(account.id),
      {
        success: $localize`:Confirmation after deleting a service account:Service account deleted`,
        error: $localize`:Error after failing to delete a service account:Service account could not be deleted`,
      }
    );
  }

  createCredential(account: ServiceAccount) {
    const dialogRef = this.dialog.open<
      CreateApiCredentialRequest,
      ServiceAccount
    >(CreateApiCredentialDialogComponent, {
      data: account,
      width: '560px',
    });

    this.mutateOnClose(
      dialogRef.closed,
      (request) => this.service.createCredential(account.id, request),
      {
        success: (credential) => {
          this.openCredentialSecret(credential);

          return $localize`:Confirmation after creating an API credential:API credential created`;
        },
        error: $localize`:Error after failing to create a credential:Credential could not be created`,
      }
    );
  }

  editCredentialScopes(account: ServiceAccount, credential: ApiCredential) {
    const dialogRef = this.dialog.open<
      UpdateApiCredentialScopesRequest,
      EditApiCredentialScopesDialogData
    >(EditApiCredentialScopesDialogComponent, {
      data: { account, credential },
      width: '600px',
    });

    this.mutateOnClose(
      dialogRef.closed,
      (request) => {
        return this.service.updateCredentialScopes(
          account.id,
          credential.id,
          request
        );
      },
      {
        success: $localize`:Confirmation after saving a credential's scopes:Credential scopes updated`,
        error: $localize`:Error after failing to save a credential's scopes:Credential scopes could not be updated`,
      }
    );
  }

  revokeCredential(account: ServiceAccount, credential: ApiCredential) {
    const confirmed = this.confirmation.open({
      title: $localize`:Title of the confirmation dialog for revoking a credential:Revoke API Credential`,
      message: $localize`:Confirmation body for revoking a credential. NAME is the credential name:Revoke "${credential.name}:NAME:"? Any agent using it will immediately lose access.`,
      acceptLabel: $localize`:Confirms revoking a credential:Revoke`,
      cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
      color: 'warn',
    });

    this.mutateOnClose(
      confirmed.pipe(map((accepted) => accepted || undefined)),
      () => this.service.revokeCredential(account.id, credential.id),
      {
        success: $localize`:Confirmation after revoking a credential:Credential revoked`,
        error: $localize`:Error after failing to revoke a credential:Credential could not be revoked`,
      }
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

  private mutateOnClose<TRequest, TResult>(
    closed: Observable<TRequest | undefined>,
    mutate: (request: TRequest) => Observable<TResult>,
    messages: MutationMessages<TResult>
  ) {
    const mutation = closed.pipe(
      first(),
      switchMap((request) => {
        if (!request) return EMPTY;

        return this.whileBusy(mutate(request));
      })
    );

    this.run(
      mutation,
      (result) => {
        const message =
          typeof messages.success === 'function'
            ? messages.success(result)
            : messages.success;

        this.snackbar.success(message);
      },
      messages.error
    );
  }

  private run<T>(
    mutation: Observable<T>,
    onSuccess: (result: T) => void,
    errorMessage: string
  ) {
    mutation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        onSuccess(result);
        this.reload();
      },
      error: () => this.snackbar.error(errorMessage),
    });
  }

  private whileBusy<T>(request: Observable<T>) {
    this.busy.set(true);

    return request.pipe(finalize(() => this.busy.set(false)));
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
