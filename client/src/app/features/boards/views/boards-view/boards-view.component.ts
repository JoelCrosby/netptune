import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
} from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import { CreateBoardComponent } from '@boards/components/create-board/create-board.component';
import { loadBoards } from '@boards/store/boards/boards.actions';
import { selectBoardsLoading } from '@boards/store/boards/boards.selectors';
import { Store } from '@ngrx/store';

@Component({
    templateUrl: './boards-view.component.html',
    styleUrls: ['./boards-view.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class BoardsViewComponent implements AfterViewInit {
  loading$ = this.store.select(selectBoardsLoading);

  constructor(
    private dialog: DialogService,
    private store: Store
  ) {}

  onCreateBoardClicked() {
    this.dialog.open(CreateBoardComponent, {
      width: '600px',
    });
  }

  ngAfterViewInit() {
    this.store.dispatch(loadBoards());
  }
}
