import { selectAllBoardGroups } from './../../store/groups/board-groups.selectors';
import { BoardGroup } from '@app/core/models/board-group';
import {
  loadBoardGroups,
  createBoardGroup,
} from './../../store/groups/board-groups.actions';
import {
  loadBoards,
  createBoard,
  selectBoard,
} from './../../store/boards/boards.actions';
import { Component, OnInit } from '@angular/core';
import { Board } from '@app/core/models/board';
import { AppState } from '@core/core.state';
import { select, Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import {
  selectAllBoards,
  selectCurrentBoard,
} from './../../store/boards/boards.selectors';
import { tap, first, map } from 'rxjs/operators';
import { selectCurrentProject } from '@app/core/projects/projects.selectors';
import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';

@Component({
  templateUrl: './boards-view.component.html',
  styleUrls: ['./boards-view.component.scss'],
})
export class BoardsViewComponent implements OnInit {
  boards$: Observable<Board[]>;
  groups$: Observable<BoardGroup[]>;
  selectedBoard$: Observable<Board>;
  selectedBoardName$: Observable<string>;

  constructor(private store: Store<AppState>) {}

  ngOnInit() {
    this.boards$ = this.store.pipe(select(selectAllBoards));
    this.groups$ = this.store.pipe(select(selectAllBoardGroups));
    this.selectedBoard$ = this.store.pipe(select(selectCurrentBoard));
    this.selectedBoardName$ = this.store.pipe(
      select(selectCurrentBoard),
      map(board => board && board.name)
    );
    this.store.dispatch(loadBoards());
    this.store.dispatch(loadBoardGroups());
  }

  boardClicked(board: Board) {
    this.store.dispatch(selectBoard({ board }));
  }

  showAddModal() {}

  drop(event: CdkDragDrop<BoardGroup[]>) {
    moveItemInArray(
      event.container.data,
      event.previousIndex,
      event.currentIndex
    );
  }

  createBoard() {
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

  createBoardGroup(name: string, sortOrder: number) {
    this.store
      .select(selectCurrentBoard)
      .pipe(
        first(),
        tap(board => {
          this.store.dispatch(
            createBoardGroup({
              boardGroup: {
                name,
                sortOrder,
                boardId: board.id,
              },
            })
          );
        })
      )
      .subscribe();
  }
}
