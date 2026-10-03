import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { ConfirmationService } from '@core/services/confirmation.service';
import { LucideListFilter, LucidePlus } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PanelComponent } from '@static/components/panel.component';
import { SkeletonCardGridComponent } from '@static/components/skeleton/skeleton-card-grid.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { QueryCardComponent } from '../../components/query-card.component';
import { PinnedQueriesService } from '../../services/pinned-queries.service';
import { SavedQueriesService } from '../../services/saved-queries.service';
import { SavedQuery } from '../../models/saved-query.models';
import {
  taskQueryCatalogResource,
  savedQueriesResource,
} from '../../resources/saved-query.resource';
import { EMPTY, switchMap } from 'rxjs';

@Component({
  selector: 'app-queries-view',
  imports: [
    RouterLink,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PanelComponent,
    ErrorStateComponent,
    EmptyStateComponent,
    FlatButtonComponent,
    SkeletonCardGridComponent,
    QueryCardComponent,
    LucidePlus,
    LucideListFilter,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for the saved query list"
        title="Queries"
        [actionTitle]="createLabel()"
        [count]="count()"
        (actionClick)="onCreate()" />

      <app-page-body scroll>
        @if (loading()) {
          <app-skeleton-card-grid
            [cards]="6"
            [gridClass]="gridClass"
            i18n-label="Accessible label while the saved query list loads"
            label="Loading queries" />
        } @else if (error()) {
          <app-error-state
            i18n-title="Shown when the saved query list fails to load"
            title="Queries could not be loaded"
            i18n-description="Advice shown when a page fails to load"
            description="Check your connection and try again."
            (retry)="reload()" />
        } @else if (savedQueries().length) {
          <ul [class]="gridClass">
            @for (savedQuery of savedQueries(); track savedQuery.id) {
              <li class="min-w-0">
                <app-query-card
                  [savedQuery]="savedQuery"
                  [catalog]="catalog()"
                  [pinned]="isPinned(savedQuery.id)"
                  [canDelete]="canDelete()"
                  (pinToggled)="onTogglePin(savedQuery)"
                  (deleted)="onDelete(savedQuery)" />
              </li>
            }
          </ul>
        } @else {
          <app-panel surface="card">
            <app-empty-state
              i18n-title="Heading of the empty saved query list"
              title="No queries yet"
              i18n-description="
                Explains what saved queries do, on the empty state
              "
              description="A query pairs a set of conditions with the columns and sort you want to read it in, and can be kept private or shared with the workspace.">
              <svg emptyStateIcon lucideListFilter class="h-8 w-8"></svg>
              @if (canCreate()) {
                <a
                  emptyStateAction
                  app-flat-button
                  color="primary"
                  [routerLink]="['new']">
                  <svg lucidePlus class="h-4 w-4"></svg>
                  <span i18n="Button that opens the create-query form">
                    Create Query
                  </span>
                </a>
              }
            </app-empty-state>
          </app-panel>
        }
      </app-page-body>
    </app-page-container>
  `,
})
export class QueriesViewComponent {
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly service = inject(SavedQueriesService);
  private readonly pinned = inject(PinnedQueriesService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  // Shared by the card list and its skeleton so the two do not lay out differently.
  protected readonly gridClass = 'grid gap-4 md:grid-cols-2 xl:grid-cols-3';

  private readonly savedQueriesRef = savedQueriesResource();
  private readonly catalogRef = taskQueryCatalogResource();

  readonly savedQueries = this.savedQueriesRef.value;
  readonly catalog = this.catalogRef.value;
  readonly loading = computed(() => {
    return this.savedQueriesRef.isLoading() || this.catalogRef.isLoading();
  });
  readonly error = computed(() => Boolean(this.savedQueriesRef.error()));
  readonly count = computed(() => {
    return this.loading() ? null : this.savedQueries().length;
  });

  readonly canCreate = hasPermission(PERMISSIONS.queries.create);
  readonly canDelete = hasPermission(PERMISSIONS.queries.delete);

  readonly createLabel = computed(() => {
    return this.canCreate()
      ? $localize`:Button that opens the create-query form:Create Query`
      : null;
  });

  isPinned(queryId: number): boolean {
    return this.pinned.isPinned(queryId);
  }

  onTogglePin(savedQuery: SavedQuery) {
    this.pinned.toggle(savedQuery.id);
  }

  onCreate() {
    void this.router.navigate(['new'], { relativeTo: this.route });
  }

  onDelete(savedQuery: SavedQuery) {
    this.confirmation
      .open({
        title: $localize`:Title of the delete-query confirmation:Delete query?`,
        message: $localize`:Body of the delete-query confirmation:This removes the query for everyone it is shared with. The tasks it lists are not affected.`,
        acceptLabel: $localize`:Button that confirms deleting a query:Delete`,
        color: 'warn',
      })
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.service.delete(savedQuery.slug);
        })
      )
      .subscribe({
        next: () => {
          this.pinned.unpin(savedQuery.id);
          this.savedQueriesRef.reload();
          this.snackbar.success(
            $localize`:Confirmation that a query was deleted:Query deleted`
          );
        },
        error: () => {
          this.snackbar.error(
            $localize`:Shown when a query could not be deleted:Query could not be deleted`
          );
        },
      });
  }

  reload() {
    this.savedQueriesRef.reload();
  }
}
