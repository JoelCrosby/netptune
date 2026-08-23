import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { ConfirmationService } from '@core/services/confirmation.service';
import { visibleTaskColumnIds } from '@core/tasks/task-columns';
import {
  LucideColumns3,
  LucideListFilter,
  LucidePin,
  LucidePinOff,
  LucidePlus,
  LucideTrash2,
  LucideUsers,
} from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { QueryFieldOptionsService } from '../../services/query-field-options.service';
import { PinnedViewsService } from '../../services/pinned-views.service';
import { TaskViewsService } from '../../services/task-views.service';
import { TaskQueryGroup, TaskView } from '../../models/task-view.models';
import {
  taskQueryCatalogResource,
  taskViewsResource,
} from '../../resources/task-view.resource';
import { explainQuery } from '../../util/query-explanation';
import { EMPTY, switchMap } from 'rxjs';

@Component({
  selector: 'app-task-views-view',
  imports: [
    RouterLink,
    DatePipe,
    AvatarComponent,
    BadgeComponent,
    PageContainerComponent,
    PageHeaderComponent,
    ErrorStateComponent,
    EmptyStateComponent,
    FlatButtonComponent,
    IconButtonComponent,
    IconTileComponent,
    SkeletonComponent,
    LucidePlus,
    LucideColumns3,
    LucideListFilter,
    LucidePin,
    LucidePinOff,
    LucideTrash2,
    LucideUsers,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      @if (canCreate()) {
        <app-page-header
          i18n-title="Page title for the saved task view list"
          title="Views"
          i18n-actionTitle="Button that opens the create-view form"
          actionTitle="Create View"
          [count]="count()"
          (actionClick)="onCreate()" />
      } @else {
        <app-page-header
          i18n-title="Page title for the saved task view list"
          title="Views"
          [count]="count()" />
      }

      @if (loading()) {
        <ul
          class="grid gap-4 md:grid-cols-2 xl:grid-cols-3"
          role="status"
          i18n-aria-label="Accessible label while the saved view list loads"
          aria-label="Loading views">
          @for (card of skeletonCards; track $index) {
            <li
              class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
              <div
                class="border-border flex items-start gap-3 border-b px-5 py-4">
                <app-skeleton class="h-9 w-9 shrink-0 rounded-lg" />
                <div class="min-w-0 flex-1">
                  <app-skeleton class="h-4 w-32" />
                  <app-skeleton class="mt-2 h-3 w-20" />
                </div>
              </div>
              <div class="flex flex-col gap-3 px-5 py-4">
                <app-skeleton class="h-3 w-full" />
                <app-skeleton class="h-3 w-4/5" />
              </div>
              <div class="border-border border-t px-5 py-3">
                <app-skeleton class="h-3 w-40" />
              </div>
            </li>
          }
        </ul>
      } @else if (error()) {
        <app-error-state
          i18n-title="Shown when the saved view list fails to load"
          title="Views could not be loaded"
          i18n-description="Advice shown when a page fails to load"
          description="Check your connection and try again."
          (retry)="reload()" />
      } @else if (views().length) {
        <ul class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          @for (view of views(); track view.id) {
            <li class="min-w-0">
              <article
                class="border-border bg-card hover:border-foreground/20 flex h-full min-w-0 flex-col overflow-hidden rounded-lg border shadow-sm transition-colors">
                <header
                  class="border-border flex items-start justify-between gap-2 border-b px-5 py-4">
                  <div class="flex min-w-0 items-start gap-3">
                    <app-icon-tile [icon]="viewIcon" />

                    <div class="min-w-0">
                      <a
                        class="font-overpass text-foreground block truncate text-base font-semibold hover:underline"
                        [routerLink]="[view.slug]">
                        {{ view.name }}
                      </a>

                      <div class="mt-1 flex flex-wrap items-center gap-1.5">
                        @if (view.isShared) {
                          <app-badge color="info" class="gap-1">
                            <svg lucideUsers class="h-3 w-3"></svg>
                            <span
                              i18n="
                                Badge marking a view shared with the workspace
                              ">
                              Shared
                            </span>
                          </app-badge>
                        } @else {
                          <app-badge>
                            <span
                              i18n="
                                Badge marking a view only its owner can see
                              ">
                              Private
                            </span>
                          </app-badge>
                        }

                        @if (isPinned(view.id)) {
                          <app-badge color="primary" class="gap-1">
                            <svg lucidePin class="h-3 w-3"></svg>
                            <span
                              i18n="Badge marking a view pinned to the sidebar">
                              Pinned
                            </span>
                          </app-badge>
                        }
                      </div>
                    </div>
                  </div>

                  <div class="flex shrink-0 items-center">
                    <button
                      app-icon-button
                      type="button"
                      [attr.aria-pressed]="isPinned(view.id)"
                      [attr.aria-label]="pinLabel(view)"
                      [title]="pinLabel(view)"
                      (click)="onTogglePin(view)">
                      @if (isPinned(view.id)) {
                        <svg lucidePin class="text-primary h-4 w-4"></svg>
                      } @else {
                        <svg lucidePinOff class="h-4 w-4"></svg>
                      }
                    </button>

                    @if (canDelete() && view.canEdit) {
                      <button
                        app-icon-button
                        color="warn"
                        type="button"
                        i18n-aria-label="
                          Accessible label for the button that deletes a view
                        "
                        aria-label="Delete view"
                        (click)="onDelete(view)">
                        <svg lucideTrash2 class="h-4 w-4"></svg>
                      </button>
                    }
                  </div>
                </header>

                <div class="flex flex-1 flex-col gap-4 px-5 py-4">
                  @if (view.description) {
                    <p class="text-muted line-clamp-2 text-sm">
                      {{ view.description }}
                    </p>
                  }

                  <div class="min-w-0">
                    <p
                      class="text-muted text-xs font-semibold tracking-wide uppercase"
                      i18n="Eyebrow above the plain-language query summary">
                      Shows tasks where
                    </p>
                    <p class="mt-1 line-clamp-3 text-sm">
                      {{ summaryFor(view) }}
                    </p>
                  </div>

                  <div
                    class="text-muted mt-auto flex flex-wrap items-center gap-x-4 gap-y-1 text-xs">
                    <span class="flex items-center gap-1.5 tabular-nums">
                      <svg lucideListFilter class="h-3.5 w-3.5"></svg>
                      {{ filterLabel(view) }}
                    </span>
                    <span class="flex items-center gap-1.5 tabular-nums">
                      <svg lucideColumns3 class="h-3.5 w-3.5"></svg>
                      {{ columnLabel(view) }}
                    </span>
                  </div>
                </div>

                <footer
                  class="border-border text-muted flex flex-wrap items-center gap-x-2 gap-y-1 border-t px-5 py-3 text-xs">
                  @if (view.createdByDisplayName; as author) {
                    <app-avatar size="xs" [name]="author" />
                    <span class="min-w-0 truncate">{{ author }}</span>
                    <span aria-hidden="true">·</span>
                  }
                  <span>
                    {{ view.updatedAt ?? view.createdAt | date: 'mediumDate' }}
                  </span>
                </footer>
              </article>
            </li>
          }
        </ul>
      } @else {
        <div class="border-border bg-card rounded-lg border shadow-sm">
          <app-empty-state
            i18n-title="Heading of the empty saved view list"
            title="No views yet"
            i18n-description="
              Explains what saved task views do, on the empty state
            "
            description="A view pairs a saved query with the columns and sort you want to read it in, and can be kept private or shared with the workspace.">
            <svg emptyStateIcon lucideListFilter class="h-8 w-8"></svg>
            @if (canCreate()) {
              <a
                emptyStateAction
                app-flat-button
                color="primary"
                [routerLink]="['new']">
                <svg lucidePlus class="h-4 w-4"></svg>
                <span i18n="Button that opens the create-view form">
                  Create View
                </span>
              </a>
            }
          </app-empty-state>
        </div>
      }
    </app-page-container>
  `,
})
export class TaskViewsViewComponent {
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly service = inject(TaskViewsService);
  private readonly fieldOptions = inject(QueryFieldOptionsService);
  private readonly pinned = inject(PinnedViewsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly viewIcon = LucideListFilter;
  protected readonly skeletonCards = Array.from({ length: 6 });

  private readonly viewsRef = taskViewsResource();
  private readonly catalogRef = taskQueryCatalogResource();

  readonly views = this.viewsRef.value;
  readonly loading = computed(() => {
    return this.viewsRef.isLoading() || this.catalogRef.isLoading();
  });
  readonly error = computed(() => Boolean(this.viewsRef.error()));
  readonly count = computed(() => {
    return this.loading() ? null : this.views().length;
  });

  readonly canCreate = hasPermission(PERMISSIONS.taskViews.create);
  readonly canDelete = hasPermission(PERMISSIONS.taskViews.delete);

  isPinned(viewId: number): boolean {
    return this.pinned.isPinned(viewId);
  }

  pinLabel(view: TaskView): string {
    return this.isPinned(view.id)
      ? $localize`:Button that removes a view from the sidebar:Unpin from sidebar`
      : $localize`:Button that adds a view to the sidebar:Pin to sidebar`;
  }

  filterLabel(view: TaskView): string {
    const count = this.countConditions(view.definition?.query);

    if (count === 0) {
      return $localize`:Shown on a saved view that filters nothing out:No filters`;
    }

    return count === 1
      ? $localize`:Shown on a saved view with exactly one filter:1 filter`
      : $localize`:Number of filters on a saved view. COUNT is how many there are:${count}:COUNT: filters`;
  }

  columnLabel(view: TaskView): string {
    const count = visibleTaskColumnIds(
      view.definition?.display?.columns ?? []
    ).length;

    return count === 1
      ? $localize`:Shown on a saved view that displays one column:1 column`
      : $localize`:Number of columns a saved view displays. COUNT is how many there are:${count}:COUNT: columns`;
  }

  summaryFor(view: TaskView): string {
    const query = view.definition?.query;

    if (!query) return '';

    return explainQuery(query, {
      catalog: this.catalogRef.value(),
      labelFor: (fieldKey, value) => {
        const field = this.catalogRef
          .value()
          .fields.find((candidate) => candidate.key === fieldKey);

        return this.fieldOptions.labelFor(field, value);
      },
    });
  }

  onTogglePin(view: TaskView) {
    this.pinned.toggle(view.id);
  }

  onCreate() {
    void this.router.navigate(['new'], { relativeTo: this.route });
  }

  onDelete(view: TaskView) {
    this.confirmation
      .open({
        title: $localize`:Title of the delete-view confirmation:Delete view?`,
        message: $localize`:Body of the delete-view confirmation:This removes the view for everyone it is shared with. The tasks it lists are not affected.`,
        acceptLabel: $localize`:Button that confirms deleting a view:Delete`,
        color: 'warn',
      })
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.service.delete(view.slug);
        })
      )
      .subscribe({
        next: () => {
          this.pinned.unpin(view.id);
          this.viewsRef.reload();
          this.snackbar.success(
            $localize`:Confirmation that a view was deleted:View deleted`
          );
        },
        error: () => {
          this.snackbar.error(
            $localize`:Shown when a view could not be deleted:View could not be deleted`
          );
        },
      });
  }

  private countConditions(group: TaskQueryGroup | null | undefined): number {
    if (!group) return 0;

    const nested = group.groups.reduce((total, child) => {
      return total + this.countConditions(child);
    }, 0);

    return group.conditions.length + nested;
  }

  reload() {
    this.viewsRef.reload();
  }
}
