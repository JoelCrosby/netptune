import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import * as BoardSelectors from '@boards/store/boards/boards.selectors';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-boards-grid',
  templateUrl: './boards-grid.component.html',
  styleUrls: ['./boards-grid.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardsGridComponent implements OnInit {
  boards$: Observable<BoardViewModel[]>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.boards$ = this.store.select(BoardSelectors.selectAllBoards);
  }
}
