import { Component, computed, inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { writePendingSourceControlConnect } from '@core/auth/pending-source-control-connect';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  SourceControlConnection,
  SourceControlProviderInfo,
  sourceControlProviderNames,
  sourceControlProviderSlug,
} from '@core/models/source-control';
import { ConfirmationService } from '@core/services/confirmation.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { DialogService } from '@core/services/dialog.service';
import { SourceControlService } from '@core/services/source-control.service';
import { getErrorMessage } from '@core/util/error-message';
import { mutation } from '@core/util/mutation';
import { requireSuccess } from '@core/util/rxjs-operators';
import { SourceControlConnectionsComponent } from '@settings/components/source-control/source-control-connections.component';
import {
  SourceControlReposDialogComponent,
  SourceControlReposDialogData,
} from '@settings/components/source-control/source-control-repos-dialog.component';
import {
  sourceControlConnectionsResource,
  sourceControlProvidersResource,
} from '@settings/resources/source-control.resource';
import { SourceControlCommandsService } from '@settings/services/source-control-commands.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { ProviderMarkComponent } from '@static/components/provider-mark.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { first, switchMap } from 'rxjs';

@Component({
  selector: 'app-workspace-source-control-view',
  imports: [
    EmptyStateComponent,
    ErrorStateComponent,
    FlatButtonComponent,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    ProviderMarkComponent,
    SourceControlConnectionsComponent,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for the workspace source control settings"
        title="Source Control">
        @if (canManage()) {
          <div pageHeaderActions class="flex gap-2">
            @for (provider of providerList(); track provider.provider) {
              <button
                app-flat-button
                type="button"
                [disabled]="connect.pending()"
                (click)="beginConnect(provider)">
                <app-provider-mark [provider]="providerSlug(provider)" />
                <span i18n="Button that connects a source control provider"
                  >Connect {{ providerName(provider) }}</span
                >
              </button>
            }
          </div>
        }
      </app-page-header>

      <app-page-body scroll>
        <div class="flex flex-col gap-7 pb-10">
          @if (loadFailed()) {
            <app-error-state
              i18n-title="
                Shown when the source control settings cannot be loaded
              "
              title="Source control settings could not be loaded"
              (retry)="reload()" />
          } @else if (isUnavailable()) {
            <app-empty-state
              outlined
              i18n-title="
                Shown when no source control provider is configured on the
                server
              "
              title="Source control isn't set up on this Netptune instance"
              i18n-description="
                Explains who can enable source control providers
              "
              description="A server administrator has to register a provider app, such as a GitHub App, before workspaces can connect." />
          } @else {
            <app-source-control-connections
              [connections]="connectionList()"
              [canManage]="canManage()"
              (chooseRepos)="openRepos($event)"
              (disconnect)="disconnect($event)" />
          }
        </div>
      </app-page-body>
    </app-page-container>
  `,
})
export class WorkspaceSourceControlViewComponent {
  private readonly service = inject(SourceControlService);
  private readonly commands = inject(SourceControlCommandsService);
  private readonly dialog = inject(DialogService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly currentWorkspace = inject(CurrentWorkspaceService);

  protected readonly canManage = hasPermission(
    PERMISSIONS.sourceControl.manage
  );
  protected readonly providers = sourceControlProvidersResource();
  protected readonly connections = sourceControlConnectionsResource();

  private readonly connectFailedMessage = $localize`:Shown when starting a source control connection fails:The connection could not be started.`;

  protected readonly connect = mutation({
    fallbackError: this.connectFailedMessage,
  });

  protected readonly providerList = computed(() => {
    return this.providers.hasValue() ? this.providers.value() : [];
  });

  protected readonly connectionList = computed(() => {
    return this.connections.hasValue() ? this.connections.value() : [];
  });

  protected readonly loadFailed = computed(() => {
    return !!this.providers.error() || !!this.connections.error();
  });

  protected readonly isUnavailable = computed(() => {
    const hasLoaded = this.providers.hasValue() && !this.providers.isLoading();
    const hasProviders = this.providerList().length > 0;
    const hasConnections = this.connectionList().length > 0;

    return hasLoaded && !hasProviders && !hasConnections;
  });

  protected reload() {
    this.providers.reload();
    this.connections.reload();
  }

  protected providerName(provider: SourceControlProviderInfo): string {
    return sourceControlProviderNames[provider.provider];
  }

  protected providerSlug(provider: SourceControlProviderInfo): string {
    return sourceControlProviderSlug(provider.provider);
  }

  protected beginConnect(provider: SourceControlProviderInfo) {
    const workspace = this.currentWorkspace.slug();

    if (!workspace) {
      return;
    }

    this.connect.run(
      this.service.beginConnect(provider.provider).pipe(requireSuccess()),
      {
        onSuccess: (response) => {
          const redirectUrl = response.payload?.redirectUrl;

          if (!redirectUrl) {
            this.snackbar.error(this.connectFailedMessage);

            return;
          }

          writePendingSourceControlConnect({
            workspace,
            provider: provider.provider,
          });

          window.location.assign(redirectUrl);
        },
        onError: (message) => this.snackbar.error(message),
      }
    );
  }

  protected openRepos(connection: SourceControlConnection) {
    this.dialog.open<unknown, SourceControlReposDialogData>(
      SourceControlReposDialogComponent,
      { width: '640px', data: { connection } }
    );
  }

  protected disconnect(connection: SourceControlConnection) {
    this.confirmation
      .open({
        title: $localize`:Title of the disconnect source control confirmation:Disconnect ${connection.accountLogin}:account:?`,
        message: $localize`:Explains what disconnecting a source control account does:Its repositories are unlinked and Netptune stops reading their activity. To revoke access completely, also uninstall the app on the provider.`,
        acceptLabel: $localize`:Confirms disconnecting a source control account:Disconnect`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
      .pipe(
        first(),
        switchMap((confirmed) => {
          if (!confirmed) {
            return [];
          }

          return this.commands.disconnect(connection.id);
        })
      )
      .subscribe({
        next: () => {
          this.snackbar.success(
            $localize`:Shown after a source control account is disconnected:${connection.accountLogin}:account: disconnected.`
          );
        },
        error: (error: unknown) => {
          this.snackbar.error(
            getErrorMessage(
              error,
              $localize`:Shown when disconnecting a source control account fails:The account could not be disconnected.`
            )
          );
        },
      });
  }
}
