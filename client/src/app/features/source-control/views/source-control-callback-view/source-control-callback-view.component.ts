import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
  clearPendingSourceControlConnect,
  readPendingSourceControlConnect,
} from '@core/auth/pending-source-control-connect';
import { sourceControlProviderFromSlug } from '@core/models/source-control';
import { SourceControlService } from '@core/services/source-control.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { requireSuccess } from '@core/util/rxjs-operators';
import { getErrorMessage } from '@core/util/error-message';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';

@Component({
  selector: 'app-source-control-callback-view',
  imports: [ErrorStateComponent, PageLoadingComponent],
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
          (retry)="returnToSettings()" />
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
  protected readonly error = signal<string | null>(null);

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

    this.service
      .completeConnect(provider, parameters)
      .pipe(requireSuccess())
      .subscribe({
        next: (response) => {
          clearPendingSourceControlConnect();
          const account = response.payload?.accountLogin;

          this.snackbar.success(
            account
              ? $localize`:Shown after a source control account is connected:${account}:account: connected.`
              : $localize`:Shown after a source control account is connected without its name:Account connected.`
          );
          this.returnToSettings();
        },
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

  protected returnToSettings() {
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
}
