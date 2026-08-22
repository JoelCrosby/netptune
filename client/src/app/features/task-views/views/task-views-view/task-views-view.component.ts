import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { ConfirmationService } from '@core/services/confirmation.service';
import {
  LucideListFilter,
  LucidePin,
  LucidePinOff,
  LucidePlus,
  LucideTrash2,
  LucideUsers,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { QueryFieldOptionsService } from '../../services/query-field-options.service';
import { PinnedViewsService } from '../../services/pinned-views.service';
import { TaskViewsService } from '../../services/task-views.service';
import { TaskView } from '../../models/task-view.models';
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
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    ErrorStateComponent,
    EmptyStateComponent,
    FlatButtonComponent,
    IconButtonComponent,
    LucidePlus,
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
        <app-page-loading />
      } @else if (error()) {
        <app-error-state
          i18n-title="Shown when the saved view list fails to load"
          title="Views could not be loaded"
          i18n-description="Advice shown when a page fails to load"
          description="Check your connection and try again."
          (retry)="reload()" />
      } @else if (views().length) {
        <ul class="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          @for (view of views(); track view.id) {
            <li
              class="border-border bg-card flex min-w-0 flex-col gap-2 rounded-lg border p-4 shadow-xs">
              <div class="flex min-w-0 items-start justify-between gap-2">
                <a
                  class="min-w-0 font-medium hover:underline"
                  [routerLink]="[view.slug]">
                  {{ view.name }}
                </a>
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
              </div>

              @if (view.description) {
                <p class="text-foreground/60 line-clamp-2 text-sm">
                  {{ view.description }}
                </p>
              }

              <p class="text-foreground/50 line-clamp-2 text-xs">
                {{ summaryFor(view) }}
              </p>

              <div
                class="text-foreground/45 mt-auto flex flex-wrap items-center gap-3 pt-2 text-xs">
                @if (view.isShared) {
                  <span class="flex items-center gap-1">
                    <svg lucideUsers class="h-3.5 w-3.5"></svg>
                    <span i18n="Badge marking a view shared with the workspace">
                      Shared
                    </span>
                  </span>
                }
                <span>{{ view.createdByDisplayName }}</span>
                <span>{{
                  view.updatedAt ?? view.createdAt | date: 'mediumDate'
                }}</span>
              </div>
            </li>
          }
        </ul>
      } @else {
        <div class="border-border bg-card rounded border">
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

  reload() {
    this.viewsRef.reload();
  }
}
