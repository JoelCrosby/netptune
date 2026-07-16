import { Component, computed, inject } from '@angular/core';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
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
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { netptunePermissions } from '@core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { LucideKanban, LucidePlus } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';

@Component({
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    BoardsGridComponent,
    EmptyStateComponent,
    FlatButtonComponent,
    LucideKanban,
    LucidePlus,
  ],
  template: `<app-page-container
    [verticalPadding]="false"
    [fullHeight]="true"
    [centerPage]="true"
    [marginBottom]="true">
    @if (canCreateBoards()) {
      <app-page-header
        title="Boards"
        actionTitle="Create Board"
        [count]="count()"
        (actionClick)="onCreateBoardClicked()" />
    } @else {
      <app-page-header title="Boards" [count]="count()" />
    }

    @if (loading()) {
      <app-page-loading />
    } @else if (boards().length === 0) {
      <app-empty-state
        title="There are currently no boards."
        description="Create your first board to organise and track work for a project.">
        <svg emptyStateIcon size="38" lucideKanban></svg>

        @if (canCreateBoards()) {
          <button
            emptyStateAction
            app-flat-button
            type="button"
            (click)="onCreateBoardClicked()">
            <svg size="20" lucidePlus></svg>
            <span>Create Board</span>
          </button>
        }
      </app-empty-state>
    } @else {
      <app-boards-grid />
    }
  </app-page-container> `,
})
export class BoardsViewComponent {
  private dialog = inject(DialogService);
  private store = inject(Store);

  loading = this.store.selectSignal(selectBoardsLoading);
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
