import { Component, computed, inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { BoardsGridComponent } from '@boards/components/boards-grid/boards-grid.component';
import { CreateBoardComponent } from '@boards/components/create-board/create-board.component';
import { workspaceBoardsResource } from '@core/resources/board.resource';
import { DialogService } from '@core/services/dialog.service';
import { delayedLoading } from '@core/util/delayed-loading';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { SkeletonCardGridComponent } from '@static/components/skeleton/skeleton-card-grid.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PERMISSIONS } from '@core/auth/permissions';
import { LucideKanban, LucidePlus } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';

@Component({
  selector: 'app-boards-view',
  imports: [
    SkeletonCardGridComponent,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    BoardsGridComponent,
    EmptyStateComponent,
    FlatButtonComponent,
    LucideKanban,
    LucidePlus,
  ],
  template: `
    <app-page-container layout="list">
      @if (canCreateBoards()) {
        <app-page-header
          toolbar
          i18n-title="Page title for the board list"
          title="Boards"
          i18n-actionTitle="Button that opens the create-board dialog"
          actionTitle="Create Board"
          [count]="count()"
          (actionClick)="onCreateBoardClicked()" />
      } @else {
        <app-page-header
          toolbar
          i18n-title="Page title for the board list"
          title="Boards"
          [count]="count()" />
      }

      <app-page-body scroll>
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
          <app-boards-grid [groups]="boards()" />
        }
      </app-page-body>
    </app-page-container>
  `,
})
export class BoardsViewComponent {
  private dialog = inject(DialogService);

  readonly boardsResource = workspaceBoardsResource();

  loading = this.boardsResource.isLoading;
  showSkeleton = delayedLoading(this.loading);
  boards = this.boardsResource.value;
  count = computed(() => (this.loading() ? null : this.boards().length));

  canCreateBoards = hasPermission(PERMISSIONS.boards.create);

  onCreateBoardClicked() {
    this.dialog.open(CreateBoardComponent, {
      width: '600px',
    });
  }
}
