import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { netptunePermissions } from '@core/auth/permissions';
import {
  ApiCredential,
  ApiCredentialCreated,
  CreateApiCredentialRequest,
  ServiceAccount,
} from '@core/models/service-account';
import { ConfirmationService } from '@core/services/confirmation.service';
import { DialogService } from '@core/services/dialog.service';
import { ServiceAccountsService } from '@core/services/service-accounts.service';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import {
  LucideBot,
  LucideKeyRound,
  LucidePlus,
  LucideRotateCcwKey,
  LucideShieldCheck,
  LucideTrash2,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { EMPTY, catchError, finalize, first, map, of, switchMap } from 'rxjs';
import { ApiCredentialSecretDialogComponent } from '@settings/components/service-accounts/api-credential-secret-dialog.component';
import { CreateApiCredentialDialogComponent } from '@settings/components/service-accounts/create-api-credential-dialog.component';
import {
  CreateServiceAccountDialogComponent,
  CreateServiceAccountWizardResult,
} from '@settings/components/service-accounts/create-service-account-dialog.component';
import { permissionLabel } from '@settings/components/service-accounts/service-account-permissions';

@Component({
  selector: 'app-service-accounts-view',
  imports: [
    LucideBot,
    LucideKeyRound,
    LucidePlus,
    LucideRotateCcwKey,
    LucideShieldCheck,
    LucideTrash2,
    BadgeComponent,
    FlatButtonComponent,
    IconButtonComponent,
    StrokedButtonComponent,
    EmptyStateComponent,
    PageLoadingComponent,
    SectionHeaderComponent,
    TooltipDirective,
  ],
  template: `<section>
    <app-section-header
      heading="Service accounts"
      description="Create workspace identities for agents and integrations without sharing a user login.">
      @if (canCreate()) {
        <button
          sectionHeaderActions
          app-flat-button
          type="button"
          [disabled]="busy()"
          (click)="openCreateAccount()">
          <svg lucidePlus class="h-4 w-4"></svg>
          Create service account
        </button>
      }
    </app-section-header>

    @if (loading()) {
      <app-page-loading class="min-h-48" label="Loading service accounts" />
    } @else if (loadError()) {
      <div class="border-warn/30 bg-warn/5 rounded border p-5 text-center">
        <p class="text-warn mb-4 text-sm">{{ loadError() }}</p>
        <button app-stroked-button type="button" (click)="load()">
          Try again
        </button>
      </div>
    } @else {
      <div class="flex flex-col gap-4">
        @for (account of sortedAccounts(); track account.id) {
          <article class="border-border bg-card rounded border shadow-sm">
            <header
              class="border-border flex flex-wrap items-start justify-between gap-4 border-b px-5 py-4">
              <div class="flex min-w-0 items-start gap-3">
                <div
                  class="bg-primary/10 text-primary flex h-10 w-10 shrink-0 items-center justify-center rounded">
                  <svg lucideBot class="h-5 w-5"></svg>
                </div>
                <div class="min-w-0">
                  <div class="flex flex-wrap items-center gap-2">
                    <h3 class="font-overpass text-lg font-medium">
                      {{ account.name }}
                    </h3>
                    @if (account.disabledAt) {
                      <app-badge color="warn">Disabled</app-badge>
                    } @else {
                      <app-badge color="success">Active</app-badge>
                    }
                  </div>
                  @if (account.description) {
                    <p class="text-muted-foreground mt-1 text-sm">
                      {{ account.description }}
                    </p>
                  }
                </div>
              </div>

              @if (canManageCredentials() && !account.disabledAt) {
                <button
                  app-stroked-button
                  type="button"
                  [disabled]="busy()"
                  (click)="openCreateCredential(account)">
                  <svg lucideKeyRound class="h-4 w-4"></svg>
                  Create credential
                </button>
              }
            </header>

            <div class="grid gap-6 px-5 py-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)]">
              <div>
                <h4 class="mb-3 flex items-center gap-2 text-sm font-medium">
                  <svg lucideShieldCheck class="h-4 w-4"></svg>
                  Account permissions
                </h4>
                <div class="flex flex-wrap gap-2">
                  @for (permission of account.permissions; track permission) {
                    <app-badge shape="rounded">
                      {{ getPermissionLabel(permission) }}
                    </app-badge>
                  } @empty {
                    <span class="text-muted-foreground text-sm">
                      No permissions granted
                    </span>
                  }
                </div>
              </div>

              <div>
                <h4 class="mb-3 flex items-center gap-2 text-sm font-medium">
                  <svg lucideRotateCcwKey class="h-4 w-4"></svg>
                  Credentials
                </h4>
                <div class="border-border divide-border divide-y rounded border">
                  @for (credential of account.credentials; track credential.id) {
                    <div class="flex items-center gap-3 px-3 py-3">
                      <div class="min-w-0 flex-1">
                        <div class="flex flex-wrap items-center gap-2">
                          <span class="truncate text-sm font-medium">
                            {{ credential.name }}
                          </span>
                          <code class="text-muted-foreground text-xs">
                            {{ credential.tokenPrefix }}…
                          </code>
                          <app-badge [color]="credentialColor(credential)">
                            {{ credentialStatus(credential) }}
                          </app-badge>
                        </div>
                        <p class="text-muted-foreground mt-1 text-xs">
                          Expires {{ formatDate(credential.expiresAt) }}
                          @if (credential.lastUsedAt) {
                            · Last used {{ formatDate(credential.lastUsedAt) }}
                          } @else {
                            · Never used
                          }
                        </p>
                      </div>

                      @if (canManageCredentials() && !credential.revokedAt) {
                        <button
                          app-icon-button
                          color="warn"
                          type="button"
                          appTooltip="Revoke credential"
                          [attr.aria-label]="'Revoke ' + credential.name"
                          [disabled]="busy()"
                          (click)="revokeCredential(account, credential)">
                          <svg lucideTrash2 class="h-4 w-4"></svg>
                        </button>
                      }
                    </div>
                  } @empty {
                    <p class="text-muted-foreground px-3 py-4 text-sm">
                      No credentials have been created.
                    </p>
                  }
                </div>
              </div>
            </div>
          </article>
        } @empty {
          <div class="border-border bg-card rounded border">
            <app-empty-state
              title="No service accounts"
              description="Create an identity for Codex or another integration, then issue a scoped credential.">
              <svg emptyStateIcon lucideBot class="h-8 w-8"></svg>
              @if (canCreate()) {
                <button
                  emptyStateAction
                  app-flat-button
                  type="button"
                  (click)="openCreateAccount()">
                  <svg lucidePlus class="h-4 w-4"></svg>
                  Create service account
                </button>
              }
            </app-empty-state>
          </div>
        }
      </div>
    }
  </section>`,
})
export class ServiceAccountsViewComponent {
  private readonly service = inject(ServiceAccountsService);
  private readonly dialog = inject(DialogService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly store = inject(Store);
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

  readonly canCreate = this.store.selectSignal(
    selectHasPermission(netptunePermissions.serviceAccounts.create)
  );
  readonly canManageCredentials = this.store.selectSignal(
    selectHasPermission(
      netptunePermissions.serviceAccounts.manageCredentials
    )
  );

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

  openCreateAccount() {
    const dialogRef = this.dialog.openWizard<CreateServiceAccountWizardResult>(
      CreateServiceAccountDialogComponent,
      {
        title: 'Create Service Account',
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
        error: () => this.snackbar.error('Service account could not be created'),
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
          return this.service.createCredential(account.id, request).pipe(
            finalize(() => this.busy.set(false))
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (credential) => {
          this.snackbar.success('API credential created');
          this.openCredentialSecret(credential);
          this.load();
        },
        error: () => this.snackbar.error('Credential could not be created'),
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
        title: 'Revoke API Credential',
        message: `Revoke "${credential.name}"? Any agent using it will immediately lose access.`,
        acceptLabel: 'Revoke',
        cancelLabel: 'Cancel',
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
          this.snackbar.success('Credential revoked');
          this.load();
        },
        error: () => this.snackbar.error('Credential could not be revoked'),
      });
  }

  getPermissionLabel(permission: ApiCredential['scopes'][number]) {
    return permissionLabel(permission);
  }

  credentialStatus(credential: ApiCredential) {
    if (credential.revokedAt) return 'Revoked';
    if (new Date(credential.expiresAt).getTime() <= Date.now()) return 'Expired';
    return 'Active';
  }

  credentialColor(credential: ApiCredential) {
    return this.credentialStatus(credential) === 'Active' ? 'success' : 'warn';
  }

  formatDate(value: string) {
    return new Intl.DateTimeFormat(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }
}
