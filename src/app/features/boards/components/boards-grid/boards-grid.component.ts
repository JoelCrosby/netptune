import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnInit,
} from '@angular/core';
import { BoardViewModel } from '@app/core/models/view-models/board-view-model';
import * as BoardActions from '@boards/store/boards/boards.actions';
import * as BoardSelectors from '@boards/store/boards/boards.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-boards-grid',
  templateUrl: './boards-grid.component.html',
  styleUrls: ['./boards-grid.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardsGridComponent implements OnInit, AfterViewInit {
  boards$: Observable<BoardViewModel[]>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.boards$ = this.store.select(BoardSelectors.selectAllBoards);
  }

  ngAfterViewInit() {
    this.store.dispatch(BoardActions.loadBoards());
  }
}
