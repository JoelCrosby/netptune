import {
  Component,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { AuthPageContainerComponent } from '@app/features/auth/components/auth-page-container/auth-page-container.component';
import { WorkspaceListComponent } from '@app/features/workspaces/components/workspace-list.component';
import { WorkspacesHeaderComponent } from '@app/features/workspaces/components/workspaces-header.component';
import { DialogService } from '@core/services/dialog.service';
import { SessionService } from '@core/services/session.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { LucidePlus } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { FilterInputComponent } from '@static/components/filter-input/filter-input.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';

@Component({
  selector: 'app-workspaces-view',
  imports: [
    AuthPageContainerComponent,
    ErrorStateComponent,
    FilterInputComponent,
    LucidePlus,
    PageLoadingComponent,
    StrokedButtonComponent,
    WorkspaceListComponent,
    WorkspacesHeaderComponent,
  ],
  template: `
    <app-auth-page-container layout="narrow" align="start">
      <div class="mx-auto flex w-full max-w-180 flex-col gap-6.5">
        <app-workspaces-header
          i18n-heading="Page title for the workspace picker"
          heading="Workspaces"
          [eyebrow]="signedInAs()">
          <ng-container headerSubtitle>
            @if (singleWorkspace()) {
              <ng-container
                i18n="Subhead shown when the user has one workspace">
                One workspace, ready when you are.
              </ng-container>
            } @else {
              <ng-container
                i18n="Subhead shown above the list of the user's workspaces">
                Pick up where you left off, or jump somewhere else.
              </ng-container>
            }
          </ng-container>

          <button
            app-stroked-button
            color="neutral"
            class="shrink-0"
            type="button"
            headerActions
            (click)="openWorkspaceDialog()">
            <svg lucidePlus class="h-4 w-4" aria-hidden="true"></svg>
            <span i18n="Button that opens the create-workspace dialog">
              Create workspace
            </span>
          </button>
        </app-workspaces-header>

        @if (loading() && !loaded()) {
          <app-page-loading />
        } @else if (loadError() && !loaded()) {
          <app-error-state
            i18n-title="Shown when the workspace list fails to load"
            title="Your workspaces could not be loaded"
            i18n-description="Advice shown when a page fails to load"
            description="Check your connection and try again."
            (retry)="reload()" />
        } @else {
          @if (canFilter()) {
            <app-filter-input
              i18n-placeholder="Placeholder of the workspace filter"
              placeholder="Filter workspaces"
              [(value)]="query" />
          }

          <app-workspace-list [query]="query()" />
        }
      </div>
    </app-auth-page-container>
  `,
})
export class WorkspacesViewComponent {
  private readonly dialog = inject(DialogService);
  private readonly list = inject(WorkspaceListService);
  private readonly preferences = inject(UserPreferencesService);
  private readonly session = inject(SessionService);

  readonly loading = this.list.loading;
  readonly loadError = this.list.loadError;

  protected readonly loaded = this.list.loaded;
  protected readonly query = signal('');

  protected readonly singleWorkspace = computed(
    () => this.workspaces().length === 1
  );

  protected readonly canFilter = computed(() => this.workspaces().length > 1);

  protected readonly signedInAs = computed(() => {
    const email = this.session.currentUser()?.email;

    if (!email) return null;

    return $localize`:Label above the workspace picker naming the signed-in account. EMAIL is its address:Signed in as ${email}:EMAIL:`;
  });

  private readonly workspaces = this.list.workspaces;
  private initialSetupOpened = false;

  constructor() {
    // This page renders outside the app shell, which is what normally loads the
    // preferences the pinned ordering reads.
    this.preferences.ensureLoaded();

    effect(() => {
      if (
        !this.loaded() ||
        this.workspaces().length > 0 ||
        this.initialSetupOpened
      ) {
        return;
      }

      this.initialSetupOpened = true;
      untracked(() => this.openWorkspaceDialog());
    });
  }

  reload() {
    this.list.reload();
  }

  openWorkspaceDialog() {
    this.dialog.openWizard(WorkspaceDialogComponent, {
      title: $localize`:Title of a dialog or section:Create Workspace`,
      data: null,
      width: '720px',
    });
  }
}
