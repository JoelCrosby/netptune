import {
  Component,
  DestroyRef,
  LOCALE_ID,
  computed,
  inject,
  signal,
} from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  ApiCredential,
  ApiCredentialCreated,
  CreateApiCredentialRequest,
  ServiceAccount,
  UpdateServiceAccountRequest,
} from '@core/models/service-account';
import { ConfirmationService } from '@core/services/confirmation.service';
import { DialogService } from '@core/services/dialog.service';
import { ServiceAccountsService } from '@core/services/service-accounts.service';
import {
  LucideBot,
  LucideKeyRound,
  LucidePlus,
  LucideSettings2,
  LucideShieldCheck,
  LucideTrash,
  LucideX,
} from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { EMPTY, catchError, finalize, first, map, of, switchMap } from 'rxjs';
import { ApiCredentialSecretDialogComponent } from '@settings/components/service-accounts/api-credential-secret-dialog.component';
import { CreateApiCredentialDialogComponent } from '@settings/components/service-accounts/create-api-credential-dialog.component';
import {
  CreateServiceAccountDialogComponent,
  CreateServiceAccountWizardResult,
} from '@settings/components/service-accounts/create-service-account-dialog.component';
import {
  EditServiceAccountDialogComponent,
  EditServiceAccountDialogData,
} from '@settings/components/service-accounts/edit-service-account-dialog.component';
import { permissionLabel } from '@settings/components/service-accounts/service-account-permissions';

@Component({
  selector: 'app-service-accounts-view',
  imports: [
    ErrorStateComponent,
    LucideBot,
    IconTileComponent,
    LucideKeyRound,
    LucidePlus,
    LucideSettings2,
    LucideShieldCheck,
    LucideTrash,
    LucideX,
    BadgeComponent,
    FlatButtonComponent,
    IconButtonComponent,
    StrokedButtonComponent,
    EmptyStateComponent,
    PageLoadingComponent,
    PageContainerComponent,
    PageHeaderComponent,
    TooltipDirective,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      @if (canCreate()) {
        <app-page-header
          i18n-title="Page title for workspace service accounts"
          title="Service accounts"
          i18n-actionTitle="Button that opens the create-service-account dialog"
          actionTitle="Create service account"
          (actionClick)="openCreateAccount()" />
      } @else {
        <app-page-header
          i18n-title="Page title for workspace service accounts"
          title="Service accounts" />
      }

      <p
        class="text-muted mb-4 max-w-3xl text-sm"
        i18n="Explains what service accounts are for">
        Create workspace identities for agents and integrations without sharing
        a user login.
      </p>

      @if (loading()) {
        <app-page-loading
          class="min-h-48"
          i18n-label="Shown while service accounts are loading"
          label="Loading service accounts" />
      } @else if (loadError()) {
        <app-error-state
          compact
          i18n-title="Shown when the service account list fails to load"
          title="Service accounts could not be loaded"
          [description]="loadError() ?? ''"
          (retry)="load()" />
      } @else {
        <div class="flex flex-col gap-4">
          @for (account of sortedAccounts(); track account.id) {
            <article class="border-border bg-card rounded border shadow-sm">
              <header
                class="border-border flex flex-wrap items-start justify-between gap-4 border-b px-5 py-4">
                <div class="flex min-w-0 items-start gap-3">
                  <app-icon-tile size="large" [icon]="accountIcon" />
                  <div class="min-w-0">
                    <div class="flex flex-wrap items-center gap-2">
                      <h3 class="font-overpass text-lg font-medium">
                        {{ account.name }}
                      </h3>
                      @if (account.disabledAt) {
                        <app-badge
                          color="warn"
                          i18n="Badge marking a disabled service account">
                          Disabled
                        </app-badge>
                      } @else {
                        <app-badge
                          color="success"
                          i18n="Badge marking an enabled service account">
                          Active
                        </app-badge>
                      }
                    </div>
                    @if (account.description) {
                      <p class="text-muted mt-1 text-sm">
                        {{ account.description }}
                      </p>
                    }
                  </div>
                </div>

                @if (!account.disabledAt) {
                  <div class="flex items-center gap-2">
                    @if (canManageCredentials()) {
                      <button
                        app-stroked-button
                        type="button"
                        [disabled]="busy()"
                        (click)="openCreateCredential(account)">
                        <svg lucideKeyRound class="h-4 w-4"></svg>
                        <span
                          i18n="
                            Button that issues a new credential for a service
                            account
                          ">
                          Create credential
                        </span>
                      </button>
                    }

                    @if (canUpdate()) {
                      <button
                        app-icon-button
                        type="button"
                        i18n-appTooltip="
                          Tooltip on the button that edits a service account
                        "
                        appTooltip="Edit service account"
                        [attr.aria-label]="editAccountLabel(account.name)"
                        [disabled]="busy()"
                        (click)="openEditAccount(account)">
                        <svg lucideSettings2 class="h-4 w-4"></svg>
                      </button>
                    }

                    @if (canDelete()) {
                      <button
                        app-icon-button
                        color="warn"
                        type="button"
                        i18n-appTooltip="
                          Tooltip on the button that deletes a service account
                        "
                        appTooltip="Delete service account"
                        [attr.aria-label]="deleteAccountLabel(account.name)"
                        [disabled]="busy()"
                        (click)="deleteAccount(account)">
                        <svg lucideX class="h-4 w-4"></svg>
                      </button>
                    }
                  </div>
                }
              </header>

              <div
                class="grid gap-6 px-5 py-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)]">
                <div>
                  <h4 class="mb-3 flex items-center gap-2 text-sm font-medium">
                    <svg lucideShieldCheck class="h-4 w-4"></svg>
                    <span
                      i18n="
                        Heading above the permissions granted to a service
                        account
                      ">
                      Account permissions
                    </span>
                  </h4>
                  <div class="flex flex-wrap gap-2">
                    @for (permission of account.permissions; track permission) {
                      <app-badge shape="rounded">
                        {{ getPermissionLabel(permission) }}
                      </app-badge>
                    } @empty {
                      <span class="text-muted text-sm">
                        <span
                          i18n="
                            Shown when a service account has no permissions
                          ">
                          No permissions granted
                        </span>
                      </span>
                    }
                  </div>
                </div>

                <div>
                  <h4 class="mb-3 flex items-center gap-2 text-sm font-medium">
                    <svg lucideKeyRound class="h-4 w-4"></svg>
                    <span i18n="Heading above a service account's credentials">
                      Credentials
                    </span>
                  </h4>
                  <div
                    class="border-border divide-border divide-y rounded border">
                    @for (
                      credential of account.credentials;
                      track credential.id
                    ) {
                      <div class="flex items-center gap-3 px-3 py-3">
                        <div class="min-w-0 flex-1">
                          <div class="flex flex-wrap items-center gap-4">
                            <span class="truncate text-sm font-medium">
                              {{ credential.name }}
                            </span>
                            <code class="text-muted text-xs">
                              {{ credential.tokenPrefix }}…
                            </code>
                            <app-badge [color]="credentialColor(credential)">
                              {{ credentialStatusLabel(credential) }}
                            </app-badge>
                          </div>
                          <p class="text-muted mt-2 text-xs">
                            <span
                              i18n="
                                Credential expiry. DATE is a formatted date and
                                time
                              ">
                              Expires
                              {{
                                formatDate(credential.expiresAt) // i18n(ph="DATE")
                              }}
                            </span>
                            @if (credential.lastUsedAt) {
                              <span
                                i18n="
                                  When a credential was last used, shown after
                                  the expiry. Keep the leading separator. DATE
                                  is a formatted date and time
                                ">
                                · Last used
                                {{
                                  formatDate(credential.lastUsedAt) // i18n(ph="DATE")
                                }}
                              </span>
                            } @else {
                              <span
                                i18n="
                                  Shown after the expiry when a credential has
                                  never been used. Keep the leading separator
                                ">
                                · Never used
                              </span>
                            }
                          </p>
                        </div>

                        @if (canManageCredentials() && !credential.revokedAt) {
                          <button
                            app-icon-button
                            color="warn"
                            type="button"
                            i18n-appTooltip="
                              Tooltip on the button that revokes a credential
                            "
                            appTooltip="Revoke credential"
                            [attr.aria-label]="
                              revokeCredentialLabel(credential.name)
                            "
                            [disabled]="busy()"
                            (click)="revokeCredential(account, credential)">
                            <svg lucideTrash class="h-4 w-4"></svg>
                          </button>
                        }
                      </div>
                    } @empty {
                      <p class="text-muted px-3 py-4 text-sm">
                        <span
                          i18n="
                            Shown when a service account has no credentials
                          ">
                          No credentials have been created.
                        </span>
                      </p>
                    }
                  </div>
                </div>
              </div>
            </article>
          } @empty {
            <div class="border-border bg-card rounded border">
              <app-empty-state
                i18n-title="Heading of the empty service account list"
                title="No service accounts"
                i18n-description="
                  Explains what to do on the empty service account list
                "
                description="Create an identity for Codex or another integration, then issue a scoped credential.">
                <svg emptyStateIcon lucideBot class="h-8 w-8"></svg>
                @if (canCreate()) {
                  <button
                    emptyStateAction
                    app-flat-button
                    type="button"
                    (click)="openCreateAccount()">
                    <svg lucidePlus class="h-4 w-4"></svg>
                    <span
                      i18n="
                        Button that opens the create-service-account dialog
                      ">
                      Create service account
                    </span>
                  </button>
                }
              </app-empty-state>
            </div>
          }
        </div>
      }
    </app-page-container>
  `,
})
export class ServiceAccountsViewComponent {
  protected readonly accountIcon = LucideBot;

  private readonly locale = inject(LOCALE_ID);

  private readonly service = inject(ServiceAccountsService);
  private readonly dialog = inject(DialogService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly destroyRef = inject(DestroyRef);

  readonly accounts = signal<ServiceAccount[]>([]);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly loadError = signal('');
  readonly sortedAccounts = computed(() =>
    [...this.accounts()].sort((left, right) =>
      left.name.localeCompare(right.name)
    )
  );

  readonly canCreate = hasPermission(PERMISSIONS.serviceAccounts.create);
  readonly canManageCredentials = hasPermission(
    PERMISSIONS.serviceAccounts.manageCredentials
  );
  readonly canDelete = hasPermission(PERMISSIONS.serviceAccounts.delete);
  readonly canUpdate = hasPermission(PERMISSIONS.serviceAccounts.update);

  constructor() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.loadError.set('');

    this.service
      .getAll()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (accounts) => this.accounts.set(accounts),
        error: () =>
          this.loadError.set('Service accounts could not be loaded.'),
      });
  }

  openEditAccount(account: ServiceAccount) {
    const dialogRef = this.dialog.open<
      UpdateServiceAccountRequest,
      EditServiceAccountDialogData
    >(EditServiceAccountDialogComponent, {
      data: { account },
      width: '720px',
    });

    dialogRef.closed
      .pipe(
        first(),
        switchMap((request) => {
          if (!request) return EMPTY;
          this.busy.set(true);
          return this.service
            .update(account.id, request)
            .pipe(finalize(() => this.busy.set(false)));
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.snackbar.success(
            $localize`:Confirmation after updating a service account:Service account updated`
          );
          this.load();
        },
        error: () =>
          this.snackbar.error(
            $localize`:Error after failing to update a service account:Service account could not be updated`
          ),
      });
  }

  openCreateAccount() {
    const dialogRef = this.dialog.openWizard<CreateServiceAccountWizardResult>(
      CreateServiceAccountDialogComponent,
      {
        title: $localize`:Title of the create-service-account dialog:Create Service Account`,
        width: '720px',
      }
    );

    dialogRef.closed
      .pipe(
        first(),
        switchMap((result) => {
          if (!result) return EMPTY;
          this.busy.set(true);

          return this.service.create(result.account).pipe(
            switchMap((account) => {
              this.accounts.update((accounts) => [...accounts, account]);

              if (!result.credential) {
                return of({
                  account,
                  credential: undefined,
                  credentialFailed: false,
                });
              }

              return this.service
                .createCredential(account.id, result.credential)
                .pipe(
                  map((credential) => ({
                    account,
                    credential,
                    credentialFailed: false,
                  })),
                  catchError(() =>
                    of({
                      account,
                      credential: undefined,
                      credentialFailed: true,
                    })
                  )
                );
            }),
            finalize(() => this.busy.set(false))
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: ({ credential, credentialFailed }) => {
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

          this.load();
        },
        error: () =>
          this.snackbar.error(
            $localize`:Error after failing to create a service account:Service account could not be created`
          ),
      });
  }

  openCreateCredential(account: ServiceAccount) {
    const dialogRef = this.dialog.open<
      CreateApiCredentialRequest,
      ServiceAccount
    >(CreateApiCredentialDialogComponent, {
      data: account,
      width: '560px',
    });

    dialogRef.closed
      .pipe(
        first(),
        switchMap((request) => {
          if (!request) return EMPTY;
          this.busy.set(true);
          return this.service
            .createCredential(account.id, request)
            .pipe(finalize(() => this.busy.set(false)));
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (credential) => {
          this.snackbar.success(
            $localize`:Confirmation after creating an API credential:API credential created`
          );
          this.openCredentialSecret(credential);
          this.load();
        },
        error: () =>
          this.snackbar.error(
            $localize`:Error after failing to create a credential:Credential could not be created`
          ),
      });
  }

  deleteAccount(account: ServiceAccount) {
    this.confirmation
      .open({
        title: $localize`:Title of the confirmation dialog for deleting a service account:Delete Service Account`,
        message: $localize`:Confirmation body for deleting a service account. NAME is the account name:Delete "${account.name}:NAME:"? All of its credentials will immediately stop working. The account will remain in history as disabled.`,
        acceptLabel: $localize`:Confirms a destructive action:Delete`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;
          this.busy.set(true);
          return this.service
            .delete(account.id)
            .pipe(finalize(() => this.busy.set(false)));
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.snackbar.success(
            $localize`:Confirmation after deleting a service account:Service account deleted`
          );
          this.load();
        },
        error: () =>
          this.snackbar.error(
            $localize`:Error after failing to delete a service account:Service account could not be deleted`
          ),
      });
  }

  private openCredentialSecret(credential: ApiCredentialCreated) {
    this.dialog.open<unknown, typeof credential>(
      ApiCredentialSecretDialogComponent,
      {
        data: credential,
        width: '640px',
        disableClose: true,
      }
    );
  }

  revokeCredential(account: ServiceAccount, credential: ApiCredential) {
    this.confirmation
      .open({
        title: $localize`:Title of the confirmation dialog for revoking a credential:Revoke API Credential`,
        message: $localize`:Confirmation body for revoking a credential. NAME is the credential name:Revoke "${credential.name}:NAME:"? Any agent using it will immediately lose access.`,
        acceptLabel: $localize`:Confirms revoking a credential:Revoke`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;
          this.busy.set(true);
          return this.service
            .revokeCredential(account.id, credential.id)
            .pipe(finalize(() => this.busy.set(false)));
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.snackbar.success(
            $localize`:Confirmation after revoking a credential:Credential revoked`
          );
          this.load();
        },
        error: () =>
          this.snackbar.error(
            $localize`:Error after failing to revoke a credential:Credential could not be revoked`
          ),
      });
  }

  getPermissionLabel(permission: ApiCredential['scopes'][number]) {
    return permissionLabel(permission);
  }

  /** Discriminant used for styling — see credentialStatusLabel for display text. */
  credentialStatus(credential: ApiCredential) {
    if (credential.revokedAt) return 'Revoked';
    if (new Date(credential.expiresAt).getTime() <= Date.now())
      return 'Expired';
    return 'Active';
  }

  credentialStatusLabel(credential: ApiCredential): string {
    const status = this.credentialStatus(credential);

    if (status === 'Revoked') {
      return $localize`:Badge on a credential that has been revoked:Revoked`;
    }

    if (status === 'Expired') {
      return $localize`:Badge on a credential that has passed its expiry:Expired`;
    }

    return $localize`:Badge on a credential that is currently usable:Active`;
  }

  credentialColor(credential: ApiCredential) {
    return this.credentialStatus(credential) === 'Active' ? 'success' : 'warn';
  }

  /**
   * aria-label is a binding and was assembled by concatenation, which translators
   * cannot reorder. Each state is one whole message with a NAME placeholder.
   */
  editAccountLabel(name: string): string {
    return $localize`:Accessible label for the button that edits a service account. NAME is the account name:Edit ${name}:NAME:`;
  }

  deleteAccountLabel(name: string): string {
    return $localize`:Accessible label for the button that deletes a service account. NAME is the account name:Delete ${name}:NAME:`;
  }

  revokeCredentialLabel(name: string): string {
    return $localize`:Accessible label for the button that revokes a credential. NAME is the credential name:Revoke ${name}:NAME:`;
  }

  formatDate(value: string) {
    return new Intl.DateTimeFormat(this.locale, {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }
}
