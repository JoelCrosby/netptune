import {
  loadBoards,
  createBoard,
  selectBoard,
} from './../../store/boards.actions';
import { Component, OnInit } from '@angular/core';
import { Board } from '@app/core/models/board';
import { AppState } from '@core/core.state';
import { select, Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { selectAllBoards } from './../../store/boards.selectors';
import { tap, first } from 'rxjs/operators';
import { selectCurrentProject } from '@app/core/projects/projects.selectors';

@Component({
  templateUrl: './boards-view.component.html',
  styleUrls: ['./boards-view.component.scss'],
})
export class BoardsViewComponent implements OnInit {
  boards$: Observable<Board[]>;

  constructor(private store: Store<AppState>) {}

  ngOnInit() {
    this.boards$ = this.store.pipe(select(selectAllBoards));
    this.store.dispatch(loadBoards());
  }

  boardClicked(board: Board) {
    this.store.dispatch(selectBoard({ board }));
  }

  showAddModal() {
    this.store
      .select(selectCurrentProject)
      .pipe(
        first(),
        tap(project => {
          this.store.dispatch(
            createBoard({
              board: {
                name: 'Todo',
                identifier: 'todo-0',
                projectId: project.id,
              },
            })
          );
        })
      )
      .subscribe();
  }
}
