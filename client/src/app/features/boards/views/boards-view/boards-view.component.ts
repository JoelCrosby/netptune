import { Component, computed, inject } from '@angular/core';
import { BoardsGridComponent } from '@boards/components/boards-grid/boards-grid.component';
import { CreateBoardComponent } from '@boards/components/create-board/create-board.component';
import { loadBoards } from '@app/core/store/boards/boards.actions';
import {
  selectAllBoards,
  selectBoardsLoading,
} from '@app/core/store/boards/boards.selectors';
import { DialogService } from '@core/services/dialog.service';
import { dispatchForWorkspace } from '@core/util/dispatch-for-workspace';
import { Store } from '@ngrx/store';
import { delayedLoading } from '@core/util/delayed-loading';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { SkeletonCardGridComponent } from '@static/components/skeleton/skeleton-card-grid.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { netptunePermissions } from '@core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { LucideKanban, LucidePlus } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';

@Component({
  imports: [
    SkeletonCardGridComponent,
    PageContainerComponent,
    PageHeaderComponent,
    BoardsGridComponent,
    EmptyStateComponent,
    FlatButtonComponent,
    LucideKanban,
    LucidePlus,
  ],
  template: `
    <app-page-container
      [verticalPadding]="false"
      [fullHeight]="true"
      [centerPage]="true"
      [marginBottom]="true">
      @if (canCreateBoards()) {
        <app-page-header
          i18n-title="Page title for the board list"
          title="Boards"
          i18n-actionTitle="Button that opens the create-board dialog"
          actionTitle="Create Board"
          [count]="count()"
          (actionClick)="onCreateBoardClicked()" />
      } @else {
        <app-page-header
          i18n-title="Page title for the board list"
          title="Boards"
          [count]="count()" />
      }

      @if (loading()) {
        @if (showSkeleton()) {
          <app-skeleton-card-grid [cards]="6" />
        }
      } @else if (boards().length === 0) {
        <app-empty-state
          i18n-title="Heading of the empty board list"
          title="There are currently no boards."
          i18n-description="
            Explains what a board is for, on the empty board list
          "
          description="Create your first board to organise and track work for a project.">
          <svg emptyStateIcon size="38" lucideKanban></svg>

          @if (canCreateBoards()) {
            <button
              emptyStateAction
              app-flat-button
              type="button"
              (click)="onCreateBoardClicked()">
              <svg size="20" lucidePlus></svg>
              <span i18n="Button that opens the create-board dialog">
                Create Board
              </span>
            </button>
          }
        </app-empty-state>
      } @else {
        <app-boards-grid />
      }
    </app-page-container>
  `,
})
export class BoardsViewComponent {
  private dialog = inject(DialogService);
  private store = inject(Store);

  loading = this.store.selectSignal(selectBoardsLoading);
  showSkeleton = delayedLoading(this.loading);
  boards = this.store.selectSignal(selectAllBoards);
  count = computed(() => (this.loading() ? null : this.boards().length));

  canCreateBoards = this.store.selectSignal(
    selectHasPermission(netptunePermissions.boards.create)
  );

  constructor() {
    dispatchForWorkspace(() => loadBoards.init());
  }

  onCreateBoardClicked() {
    this.dialog.open(CreateBoardComponent, {
      width: '600px',
    });
  }
}
