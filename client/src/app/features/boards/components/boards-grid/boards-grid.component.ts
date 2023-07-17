import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import * as BoardSelectors from '@boards/store/boards/boards.selectors';
import { BoardsViewModel } from '@core/models/view-models/boards-view-model';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-boards-grid',
  templateUrl: './boards-grid.component.html',
  styleUrls: ['./boards-grid.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardsGridComponent implements OnInit {
  groups$!: Observable<BoardsViewModel[]>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.groups$ = this.store.select(BoardSelectors.selectAllBoards);
  }
}
