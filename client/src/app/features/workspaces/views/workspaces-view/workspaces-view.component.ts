import { Component, computed, effect, inject, untracked } from '@angular/core';
import { WorkspaceListComponent } from '@app/features/workspaces/components/workspace-list.component';
import { BuildNumberComponent } from '@app/static/components/build-number/build-number.component';
import { DialogService } from '@core/services/dialog.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { LucidePlus } from '@lucide/angular';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';

@Component({
  selector: 'app-workspaces-view',
  imports: [
    BuildNumberComponent,
    ErrorStateComponent,
    LucidePlus,
    PageContainerComponent,
    PageLoadingComponent,
    WorkspaceListComponent,
  ],
  template: `
    <app-page-container
      [centerPage]="false"
      [horizontalPadding]="false"
      [fullHeight]="false">
      <div class="mx-auto w-full max-w-195 px-4 pt-12 pb-8 sm:px-6 sm:pt-18">
        <header class="flex items-start gap-4">
          <div class="min-w-0 flex-1">
            <h1
              class="font-overpass m-0 text-3xl font-semibold tracking-[-0.01em] text-[rgb(var(--foreground-rgb))]"
              i18n="Page title for the workspace picker">
              Workspaces
            </h1>
            <p class="mt-1.5 text-sm text-[rgba(var(--foreground-rgb),0.5)]">
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
            </p>
          </div>
          <button
            type="button"
            class="border-border hover:border-foreground/22 inline-flex h-9 shrink-0 cursor-pointer items-center gap-1.75 rounded-[7px] border bg-transparent px-3.5 text-[13px] font-medium whitespace-nowrap transition-colors hover:bg-[rgba(var(--foreground-rgb),0.06)]"
            (click)="openWorkspaceDialog()">
            <svg lucidePlus class="h-3.75 w-3.75" aria-hidden="true"></svg>
            <span i18n="Button that opens the create-workspace dialog">
              New workspace
            </span>
          </button>
        </header>

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
          <div class="mt-7">
            <app-workspace-list />
          </div>
        }
      </div>

      <app-build-number />
    </app-page-container>
  `,
})
export class WorkspacesViewComponent {
  private readonly dialog = inject(DialogService);
  private readonly list = inject(WorkspaceListService);
  private readonly preferences = inject(UserPreferencesService);

  readonly loading = this.list.loading;
  readonly loadError = this.list.loadError;

  protected readonly loaded = this.list.loaded;
  protected readonly singleWorkspace = computed(
    () => this.workspaces().length === 1
  );

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
