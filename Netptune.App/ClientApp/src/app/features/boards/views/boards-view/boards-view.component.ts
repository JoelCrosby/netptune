import { selectAllBoardGroups } from './../../store/groups/board-groups.selectors';
import { BoardGroup, BoardGroupType } from '@app/core/models/board-group';
import * as actions from './../../store/groups/board-groups.actions';
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
import { getNewSortOrder } from '@app/core/util/sort-order-helper';

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
    this.store.dispatch(actions.loadBoardGroups());
  }

  boardClicked(board: Board) {
    this.store.dispatch(selectBoard({ board }));
  }

  showAddModal() {}

  getsiblingIds(group: BoardGroup, groups: BoardGroup[]): string[] {
    return groups
      .filter(item => item.id !== group.id)
      .map(item => `bg-${item.id.toString()}`);
  }

  getDragListId(group: BoardGroup) {
    return `bg-${group.id.toString()}`;
  }

  drop(event: CdkDragDrop<BoardGroup[]>) {
    moveItemInArray(
      event.container.data,
      event.previousIndex,
      event.currentIndex
    );

    const groups = event.container.data;

    const prevGroup = groups[event.currentIndex - 1];
    const nextGroup = groups[event.currentIndex + 1];

    const preOrder = prevGroup && prevGroup.sortOrder;
    const nextOrder = nextGroup && nextGroup.sortOrder;

    const order = getNewSortOrder(preOrder, nextOrder);

    const { data } = event.item;

    if (data.sortOrder === order) {
      return;
    }

    this.moveBoardGroup(data, order);
  }

  moveBoardGroup(boardGroup: BoardGroup, sortOrder: number) {
    this.store.dispatch(
      actions.editBoardGroup({
        boardGroup: {
          ...boardGroup,
          sortOrder,
        },
      })
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
            actions.createBoardGroup({
              boardGroup: {
                name,
                sortOrder,
                boardId: board.id,
                type: BoardGroupType.Basic,
              },
            })
          );
        })
      )
      .subscribe();
  }
}
