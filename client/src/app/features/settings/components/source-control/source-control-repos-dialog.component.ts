import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { LucideRefreshCw } from '@lucide/angular';
import {
  SourceControlConnection,
  SourceControlHistorySyncStatus,
  SourceControlRepo,
  sourceControlProviderNames,
} from '@core/models/source-control';
import { getErrorMessage } from '@core/util/error-message';
import { mutation } from '@core/util/mutation';
import { sourceControlReposResource } from '@settings/resources/source-control.resource';
import { SourceControlCommandsService } from '@settings/services/source-control-commands.service';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import { SelectableRowComponent } from '@static/components/selectable-row.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { SupportingTextComponent } from '@static/components/supporting-text.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { TooltipDirective } from '@static/directives/tooltip.directive';

export interface SourceControlReposDialogData {
  connection: SourceControlConnection;
}

@Component({
  selector: 'app-source-control-repos-dialog',
  imports: [
    BadgeComponent,
    DialogActionsDirective,
    DialogTitleComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    FlatButtonComponent,
    IconButtonComponent,
    LucideRefreshCw,
    SearchInputComponent,
    SelectableRowComponent,
    SkeletonComponent,
    StrokedButtonComponent,
    SupportingTextComponent,
    TooltipDirective,
  ],
  template: `
    <app-dialog-title showCloseButton [subtitle]="subtitle">
      <span i18n="Title of the repository picker dialog"
        >Choose Repositories</span
      >
    </app-dialog-title>

    <div class="flex flex-col gap-4">
      <p app-supporting-text i18n="Explains what linking a repository does">
        Linked repositories can be referenced from tasks. Netptune ignores
        activity in repositories that are not linked.
      </p>

      <app-search-input
        [term]="term()"
        (searchChange)="term.set($event ?? '')" />

      @if (repos.isLoading() && !repos.hasValue()) {
        <app-skeleton class="h-48" />
      } @else if (loadFailed()) {
        <app-error-state
          compact
          i18n-title="Shown when the repository list cannot be loaded"
          title="The repositories could not be loaded"
          (retry)="repos.reload()" />
      } @else {
        <div class="flex max-h-96 flex-col overflow-y-auto">
          @for (repo of visibleRepos(); track repo.id) {
            <div class="flex items-center gap-1">
              <app-selectable-row
                class="min-w-0 flex-1"
                [checked]="isLinked(repo)"
                [disabled]="isLocked(repo)"
                (toggled)="setLinked(repo, $event)">
                <div
                  class="flex min-w-0 flex-1 items-center justify-between gap-3">
                  <span class="truncate">{{ repo.fullName }}</span>
                  <div class="flex shrink-0 gap-1.5">
                    @if (repo.isPrivate) {
                      <app-badge i18n="Badge on a private repository"
                        >Private</app-badge
                      >
                    }
                    @if (repo.isArchived) {
                      <app-badge
                        i18n="repository state|Badge on an archived repository"
                        >Archived</app-badge
                      >
                    }
                    @if (!repo.isAccessible) {
                      <app-badge
                        color="warn"
                        i18n="
                          Badge on a repository the connection can no longer see
                        "
                        >No access</app-badge
                      >
                    }
                    @if (repo.isLinked) {
                      @switch (repo.historySyncStatus) {
                        @case (historySyncStatus.syncing) {
                          <app-badge
                            color="pending"
                            i18n="
                              Badge while a repository's recent history is being
                              imported
                            "
                            >Syncing history</app-badge
                          >
                        }
                        @case (historySyncStatus.failed) {
                          <app-badge
                            color="warn"
                            i18n="
                              Badge when importing a repository's recent history
                              failed
                            "
                            >History sync failed</app-badge
                          >
                        }
                      }
                    }
                  </div>
                </div>
              </app-selectable-row>
              @if (repo.isLinked) {
                <button
                  app-icon-button
                  type="button"
                  i18n-ariaLabel="
                    Button that imports a repository's recent pull requests and
                    branches again
                  "
                  ariaLabel="Sync history"
                  [appTooltip]="syncHistoryTooltip"
                  [disabled]="isSyncingHistory(repo)"
                  (click)="syncHistory(repo)">
                  <svg lucideRefreshCw class="h-4 w-4"></svg>
                </button>
              }
            </div>
          } @empty {
            <app-empty-state
              compact
              i18n-title="Shown when no repositories match the picker filter"
              title="No repositories found" />
          }
        </div>
      }
    </div>

    <div app-dialog-actions class="justify-between">
      <button
        app-flat-button
        color="ghost"
        type="button"
        [disabled]="refresh.pending()"
        (click)="sync()">
        <span i18n="Button that reloads the repository list from the provider"
          >Refresh List</span
        >
      </button>
      <button app-stroked-button type="button" (click)="close()">
        <span i18n="Closes the repository picker">Done</span>
      </button>
    </div>
  `,
})
export class SourceControlReposDialogComponent {
  private readonly data = inject<SourceControlReposDialogData>(DIALOG_DATA);
  private readonly dialogRef =
    inject<DialogRef<unknown, SourceControlReposDialogComponent>>(DialogRef);

  private readonly commands = inject(SourceControlCommandsService);
  private readonly snackbar = inject(SnackbarService);

  protected readonly repos = sourceControlReposResource(
    () => this.data.connection.id
  );

  protected readonly refresh = mutation({
    fallbackError: $localize`:Shown when refreshing the repository list fails:The repository list could not be refreshed.`,
  });

  protected readonly historySyncStatus = SourceControlHistorySyncStatus;
  protected readonly syncHistoryTooltip = $localize`:Tooltip on the button that imports a repository's history:Link pull requests and branches from the last 90 days`;
  private readonly historySyncRepoIds = signal<ReadonlySet<number>>(new Set());

  protected readonly subtitle = `${sourceControlProviderNames[this.data.connection.provider]} · ${this.data.connection.accountLogin}`;
  protected readonly term = signal('');
  private readonly pendingRepoIds = signal<ReadonlySet<number>>(new Set());
  private readonly linkedOverrides = signal(new Map<number, boolean>());

  protected readonly loadFailed = computed(() => !!this.repos.error());

  protected readonly visibleRepos = computed(() => {
    const repos = this.repos.hasValue() ? this.repos.value() : [];
    const term = this.term().trim().toLowerCase();

    if (!term) {
      return repos;
    }

    return repos.filter((repo) => repo.fullName.toLowerCase().includes(term));
  });

  protected isLinked(repo: SourceControlRepo): boolean {
    return this.linkedOverrides().get(repo.id) ?? repo.isLinked;
  }

  // A repository that lost access can still be unlinked, just not linked again.
  protected isLocked(repo: SourceControlRepo): boolean {
    const cannotLink = !repo.isAccessible && !this.isLinked(repo);
    const isPending = this.pendingRepoIds().has(repo.id);

    return cannotLink || isPending;
  }

  protected setLinked(repo: SourceControlRepo, isLinked: boolean) {
    this.setPending(repo.id, true);
    this.setOverride(repo.id, isLinked);

    this.commands.setLinked(repo, isLinked).subscribe({
      next: () => this.setPending(repo.id, false),
      error: (error: unknown) => {
        this.setOverride(repo.id, !isLinked);
        this.setPending(repo.id, false);
        this.snackbar.error(
          getErrorMessage(
            error,
            $localize`:Shown when linking or unlinking a repository fails:${repo.fullName}:repository: could not be updated.`
          )
        );
      },
    });
  }

  protected isSyncingHistory(repo: SourceControlRepo): boolean {
    const isRequesting = this.historySyncRepoIds().has(repo.id);
    const isRunning =
      repo.historySyncStatus === SourceControlHistorySyncStatus.syncing;

    return isRequesting || isRunning;
  }

  protected syncHistory(repo: SourceControlRepo) {
    this.historySyncRepoIds.update((ids) => new Set(ids).add(repo.id));

    this.commands.syncHistory(repo).subscribe({
      next: () => this.clearHistorySync(repo.id),
      error: (error: unknown) => {
        this.clearHistorySync(repo.id);
        this.snackbar.error(
          getErrorMessage(
            error,
            $localize`:Shown when starting a repository history sync fails:The history of ${repo.fullName}:repository: could not be synced.`
          )
        );
      },
    });
  }

  protected sync() {
    this.refresh.run(this.commands.sync(this.data.connection.id), {
      onSuccess: () => this.linkedOverrides.set(new Map()),
      onError: (message) => this.snackbar.error(message),
    });
  }

  protected close() {
    this.dialogRef.close();
  }

  private clearHistorySync(repoId: number) {
    this.historySyncRepoIds.update((ids) => {
      const next = new Set(ids);

      next.delete(repoId);

      return next;
    });
  }

  private setPending(repoId: number, isPending: boolean) {
    this.pendingRepoIds.update((pending) => {
      const next = new Set(pending);

      if (isPending) {
        next.add(repoId);
      } else {
        next.delete(repoId);
      }

      return next;
    });
  }

  private setOverride(repoId: number, isLinked: boolean) {
    this.linkedOverrides.update((overrides) => {
      const next = new Map(overrides);

      next.set(repoId, isLinked);

      return next;
    });
  }
}
