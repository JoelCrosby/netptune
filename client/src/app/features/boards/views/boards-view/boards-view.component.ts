import { Component, computed, inject } from '@angular/core';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
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

@Component({
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    SpinnerComponent,
    BoardsGridComponent,
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
      <div class="flex h-full flex-col items-center justify-center">
        <app-spinner diameter="32px" />
      </div>
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
