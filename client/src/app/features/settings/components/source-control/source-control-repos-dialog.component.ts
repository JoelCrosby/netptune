import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import {
  SourceControlConnection,
  SourceControlRepo,
  sourceControlProviderNames,
} from '@core/models/source-control';
import { sourceControlReposResource } from '@core/resources/source-control.resource';
import { SourceControlService } from '@core/services/source-control.service';
import { mutation } from '@core/util/mutation';
import { requireSuccess } from '@core/util/rxjs-operators';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
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
    SearchInputComponent,
    SelectableRowComponent,
    SkeletonComponent,
    StrokedButtonComponent,
    SupportingTextComponent,
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
            <app-selectable-row
              [checked]="isLinked(repo)"
              [disabled]="!repo.isAccessible || pendingRepoId() === repo.id"
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
                    <app-badge i18n="Badge on an archived repository"
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
                </div>
              </div>
            </app-selectable-row>
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
    inject<DialogRef<boolean, SourceControlReposDialogComponent>>(DialogRef);

  private readonly service = inject(SourceControlService);
  private readonly snackbar = inject(SnackbarService);

  protected readonly repos = sourceControlReposResource(
    () => this.data.connection.id
  );

  protected readonly refresh = mutation({
    fallbackError: $localize`:Shown when refreshing the repository list fails:The repository list could not be refreshed.`,
  });

  protected readonly subtitle = `${sourceControlProviderNames[this.data.connection.provider]} · ${this.data.connection.accountLogin}`;
  protected readonly term = signal('');
  protected readonly pendingRepoId = signal<number | null>(null);

  private readonly linkedOverrides = signal(new Map<number, boolean>());
  private changed = false;

  protected readonly loadFailed = computed(() => {
    const response = this.repos.value();

    return !!this.repos.error() || response?.isSuccess === false;
  });

  protected readonly visibleRepos = computed(() => {
    const repos = this.repos.value()?.payload ?? [];
    const term = this.term().trim().toLowerCase();

    if (!term) {
      return repos;
    }

    return repos.filter((repo) => repo.fullName.toLowerCase().includes(term));
  });

  protected isLinked(repo: SourceControlRepo): boolean {
    return this.linkedOverrides().get(repo.id) ?? repo.isLinked;
  }

  protected setLinked(repo: SourceControlRepo, isLinked: boolean) {
    this.pendingRepoId.set(repo.id);
    this.setOverride(repo.id, isLinked);

    this.service
      .setLinked(repo.id, isLinked)
      .pipe(requireSuccess())
      .subscribe({
        next: () => {
          this.changed = true;
          this.pendingRepoId.set(null);
        },
        error: () => {
          this.setOverride(repo.id, !isLinked);
          this.pendingRepoId.set(null);
          this.snackbar.error(
            $localize`:Shown when linking or unlinking a repository fails:${repo.fullName}:repository: could not be updated.`
          );
        },
      });
  }

  protected sync() {
    this.refresh.run(
      this.service.sync(this.data.connection.id).pipe(requireSuccess()),
      {
        onSuccess: () => {
          this.linkedOverrides.set(new Map());
          this.repos.reload();
        },
        onError: (message) => this.snackbar.error(message),
      }
    );
  }

  protected close() {
    this.dialogRef.close(this.changed);
  }

  private setOverride(repoId: number, isLinked: boolean) {
    this.linkedOverrides.update((overrides) => {
      const next = new Map(overrides);

      next.set(repoId, isLinked);

      return next;
    });
  }
}
