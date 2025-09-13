import { AfterViewInit, ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import { CreateBoardComponent } from '@boards/components/create-board/create-board.component';
import { loadBoards } from '@boards/store/boards/boards.actions';
import { selectBoardsLoading } from '@boards/store/boards/boards.selectors';
import { Store } from '@ngrx/store';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { AsyncPipe } from '@angular/common';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { BoardsGridComponent } from '@boards/components/boards-grid/boards-grid.component';

@Component({
  templateUrl: './boards-view.component.html',
  styleUrls: ['./boards-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    MatProgressSpinner,
    BoardsGridComponent,
    AsyncPipe
],
})
export class BoardsViewComponent implements AfterViewInit {
  private dialog = inject(DialogService);
  private store = inject(Store);

  loading$ = this.store.select(selectBoardsLoading);

  onCreateBoardClicked() {
    this.dialog.open(CreateBoardComponent, {
      width: '600px',
    });
  }

  ngAfterViewInit() {
    this.store.dispatch(loadBoards());
  }
}
