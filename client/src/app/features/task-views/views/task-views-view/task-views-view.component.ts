import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { ConfirmationService } from '@core/services/confirmation.service';
import { LucideListFilter, LucidePlus } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SkeletonCardGridComponent } from '@static/components/skeleton/skeleton-card-grid.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { TaskViewCardComponent } from '../../components/task-view-card.component';
import { PinnedViewsService } from '../../services/pinned-views.service';
import { TaskViewsService } from '../../services/task-views.service';
import { TaskView } from '../../models/task-view.models';
import {
  taskQueryCatalogResource,
  taskViewsResource,
} from '../../resources/task-view.resource';
import { EMPTY, switchMap } from 'rxjs';

@Component({
  selector: 'app-task-views-view',
  imports: [
    RouterLink,
    PageContainerComponent,
    PageHeaderComponent,
    ErrorStateComponent,
    EmptyStateComponent,
    FlatButtonComponent,
    SkeletonCardGridComponent,
    TaskViewCardComponent,
    LucidePlus,
    LucideListFilter,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for the saved task view list"
        title="Views"
        [actionTitle]="createLabel()"
        [count]="count()"
        (actionClick)="onCreate()" />

      @if (loading()) {
        <app-skeleton-card-grid
          [cards]="6"
          [gridClass]="gridClass"
          i18n-label="Accessible label while the saved view list loads"
          label="Loading views" />
      } @else if (error()) {
        <app-error-state
          i18n-title="Shown when the saved view list fails to load"
          title="Views could not be loaded"
          i18n-description="Advice shown when a page fails to load"
          description="Check your connection and try again."
          (retry)="reload()" />
      } @else if (views().length) {
        <ul [class]="gridClass">
          @for (view of views(); track view.id) {
            <li class="min-w-0">
              <app-task-view-card
                [view]="view"
                [catalog]="catalog()"
                [pinned]="isPinned(view.id)"
                [canDelete]="canDelete()"
                (pinToggled)="onTogglePin(view)"
                (deleted)="onDelete(view)" />
            </li>
          }
        </ul>
      } @else {
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
      }
    </app-page-container>
  `,
})
export class TaskViewsViewComponent {
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly service = inject(TaskViewsService);
  private readonly pinned = inject(PinnedViewsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  // Shared by the card list and its skeleton so the two do not lay out differently.
  protected readonly gridClass = 'grid gap-4 md:grid-cols-2 xl:grid-cols-3';

  private readonly viewsRef = taskViewsResource();
  private readonly catalogRef = taskQueryCatalogResource();

  readonly views = this.viewsRef.value;
  readonly catalog = this.catalogRef.value;
  readonly loading = computed(() => {
    return this.viewsRef.isLoading() || this.catalogRef.isLoading();
  });
  readonly error = computed(() => Boolean(this.viewsRef.error()));
  readonly count = computed(() => {
    return this.loading() ? null : this.views().length;
  });

  readonly canCreate = hasPermission(PERMISSIONS.taskViews.create);
  readonly canDelete = hasPermission(PERMISSIONS.taskViews.delete);

  readonly createLabel = computed(() => {
    return this.canCreate()
      ? $localize`:Button that opens the create-view form:Create View`
      : null;
  });

  isPinned(viewId: number): boolean {
    return this.pinned.isPinned(viewId);
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
