import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  inject,
} from '@angular/core';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { BoardsGridComponent } from '@boards/components/boards-grid/boards-grid.component';
import { CreateBoardComponent } from '@boards/components/create-board/create-board.component';
import { loadBoards } from '@boards/store/boards/boards.actions';
import { selectBoardsLoading } from '@boards/store/boards/boards.selectors';
import { DialogService } from '@core/services/dialog.service';
import { Store } from '@ngrx/store';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  templateUrl: './boards-view.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    MatProgressSpinner,
    BoardsGridComponent,
  ],
})
export class BoardsViewComponent implements AfterViewInit {
  private dialog = inject(DialogService);
  private store = inject(Store);

  loading = this.store.selectSignal(selectBoardsLoading);

  onCreateBoardClicked() {
    this.dialog.open(CreateBoardComponent, {
      width: '600px',
    });
  }

  ngAfterViewInit() {
    this.store.dispatch(loadBoards());
  }
}
