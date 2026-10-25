import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
  clearPendingSourceControlConnect,
  readPendingSourceControlConnect,
} from '@core/auth/pending-source-control-connect';
import {
  SourceControlConnectCandidate,
  SourceControlConnectChoice,
  SourceControlConnectCompletion,
  SourceControlProvider,
  sourceControlProviderFromSlug,
  sourceControlProviderNames,
} from '@core/models/source-control';
import { SourceControlService } from '@core/services/source-control.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { requireSuccess } from '@core/util/rxjs-operators';
import { getErrorMessage } from '@core/util/error-message';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { MediaRowComponent } from '@static/components/media-row.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { PanelComponent } from '@static/components/panel.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';

@Component({
  selector: 'app-source-control-callback-view',
  imports: [
    AvatarComponent,
    ErrorStateComponent,
    FlatButtonComponent,
    MediaRowComponent,
    PageLoadingComponent,
    PanelBodyComponent,
    PanelComponent,
    PanelHeaderComponent,
    StrokedButtonComponent,
  ],
  template: `
    <div class="flex min-h-svh items-center justify-center p-6">
      @if (error(); as message) {
        <app-error-state
          i18n-title="
            Shown when a source control connection cannot be completed
          "
          title="The connection could not be completed"
          [description]="message"
          [retryLabel]="returnLabel"
          (retry)="onReturnToSettings()" />
      } @else if (choice(); as choice) {
        <section app-panel surface="card" class="w-full max-w-lg">
          <app-panel-header
            density="comfortable"
            i18n-heading="
              Heading of the page that asks which source control account to
              connect
            "
            heading="Choose an account"
            i18n-description="
              Explains the choice between an account that already has the app
              and installing it somewhere new
            "
            description="The app is already installed on these accounts. Connect one to this workspace, or install the app on another account." />

          <div app-panel-body class="flex flex-col gap-3">
            @for (
              candidate of choice.candidates;
              track candidate.accountLogin
            ) {
              <app-media-row [heading]="candidate.accountLogin">
                <app-avatar
                  mediaRowMedia
                  size="lg"
                  [tooltip]="false"
                  [name]="candidate.accountLogin"
                  [imageUrl]="candidate.accountAvatarUrl" />

                <span>{{ providerName }}</span>

                <button
                  mediaRowActions
                  app-flat-button
                  type="button"
                  (click)="onConnectCandidate(candidate)">
                  <span
                    i18n="
                      Button that connects an account the source control app is
                      already installed on
                    "
                    >Connect</span
                  >
                </button>
              </app-media-row>
            }

            <button
              app-stroked-button
              type="button"
              class="self-start"
              (click)="onInstall(choice)">
              <span
                i18n="
                  Button that installs the source control app on an account not
                  listed
                "
                >Install on another account</span
              >
            </button>
          </div>
        </section>
      } @else {
        <app-page-loading
          i18n-label="
            Shown while a source control connection is being completed
          "
          label="Connecting" />
      }
    </div>
  `,
})
export class SourceControlCallbackViewComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(SourceControlService);
  private readonly workspaceService = inject(WorkspaceService);
  private readonly snackbar = inject(SnackbarService);

  private readonly pending = readPendingSourceControlConnect();

  protected readonly workspace = this.pending?.workspace ?? null;
  protected readonly providerName = this.pending
    ? sourceControlProviderNames[this.pending.provider]
    : '';
  protected readonly error = signal<string | null>(null);
  protected readonly choice = signal<SourceControlConnectChoice | null>(null);

  protected readonly returnLabel = this.workspace
    ? $localize`:Button that returns to the source control settings:Back to Settings`
    : $localize`:Button that goes to the list of the user's workspaces:Go to Workspaces`;

  ngOnInit() {
    const slug = this.route.snapshot.paramMap.get('provider') ?? '';
    const provider = sourceControlProviderFromSlug(slug);
    const pending = this.pending;

    if (!pending || provider === null || pending.provider !== provider) {
      clearPendingSourceControlConnect();
      this.error.set(
        $localize`:Shown when the source control callback has no matching connection request:This connection wasn't started from this browser tab. Start it again from your workspace settings.`
      );

      return;
    }

    this.workspaceService.setWorkspace(pending.workspace);

    const parameters = { ...this.route.snapshot.queryParams } as Record<
      string,
      string
    >;

    this.complete(provider, parameters);
  }

  protected onConnectCandidate(candidate: SourceControlConnectCandidate) {
    if (!this.pending) {
      return;
    }

    this.choice.set(null);
    this.complete(this.pending.provider, candidate.callbackParameters);
  }

  protected onInstall(choice: SourceControlConnectChoice) {
    window.location.assign(choice.installUrl);
  }

  protected onReturnToSettings() {
    if (!this.workspace) {
      void this.router.navigate(['/workspaces']);

      return;
    }

    void this.router.navigate([
      '/',
      this.workspace,
      'settings',
      'workspace',
      'source-control',
    ]);
  }

  private complete(
    provider: SourceControlProvider,
    parameters: Record<string, string>
  ) {
    this.service
      .completeConnect(provider, parameters)
      .pipe(requireSuccess())
      .subscribe({
        next: (response) => this.handleCompletion(response.payload),
        error: (error: unknown) => {
          clearPendingSourceControlConnect();
          this.error.set(
            getErrorMessage(
              error,
              $localize`:Fallback when completing a source control connection fails:The provider did not confirm the connection.`
            )
          );
        },
      });
  }

  // The pending connection is kept while choosing, because installing returns to this callback.
  private handleCompletion(completion?: SourceControlConnectCompletion | null) {
    const choice = completion?.choice;

    if (choice) {
      if (!choice.candidates.length) {
        this.onInstall(choice);

        return;
      }

      this.choice.set(choice);

      return;
    }

    clearPendingSourceControlConnect();
    const account = completion?.connection?.accountLogin;

    this.snackbar.success(
      account
        ? $localize`:Shown after a source control account is connected:${account}:account: connected.`
        : $localize`:Shown after a source control account is connected without its name:Account connected.`
    );
    this.onReturnToSettings();
  }
}
