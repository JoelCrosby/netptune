import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
} from '@angular/core';
import { MatLegacyDialog as MatDialog } from '@angular/material/legacy-dialog';
import { CreateBoardComponent } from '@boards/components/create-board/create-board.component';
import { loadBoards } from '@boards/store/boards/boards.actions';
import { selectBoardsLoading } from '@boards/store/boards/boards.selectors';
import { Store } from '@ngrx/store';

@Component({
  templateUrl: './boards-view.component.html',
  styleUrls: ['./boards-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardsViewComponent implements AfterViewInit {
  loading$ = this.store.select(selectBoardsLoading);

  constructor(
    private dialog: MatDialog,
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
