import { BoardGroup, BoardGroupType } from '@app/core/models/board-group';
import * as GroupActions from '@boards/store/groups/board-groups.actions';
import * as GroupSelectors from '@boards/store/groups/board-groups.selectors';
import * as BoardActions from '@boards/store/boards/boards.actions';
import * as BoardSelectors from '@boards/store/boards/boards.selectors';
import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Board } from '@app/core/models/board';
import { AppState } from '@core/core.state';
import { select, Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { tap, first, map } from 'rxjs/operators';
import { selectCurrentProject } from '@app/core/projects/projects.selectors';
import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { getNewSortOrder } from '@app/core/util/sort-order-helper';

@Component({
  templateUrl: './boards-view.component.html',
  styleUrls: ['./boards-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardsViewComponent implements OnInit {
  boards$: Observable<Board[]>;
  groups$: Observable<BoardGroup[]>;
  selectedBoard$: Observable<Board>;
  selectedBoardName$: Observable<string>;

  constructor(private store: Store<AppState>) {}

  ngOnInit() {
    this.boards$ = this.store.select(BoardSelectors.selectAllBoards);
    this.groups$ = this.store.select(GroupSelectors.selectAllBoardGroups);
    this.selectedBoard$ = this.store.select(BoardSelectors.selectCurrentBoard);

    this.selectedBoardName$ = this.store.pipe(
      select(BoardSelectors.selectCurrentBoard),
      map((board) => board && board.name)
    );

    this.store.dispatch(BoardActions.loadBoards());
    this.store.dispatch(GroupActions.loadBoardGroups());
  }

  boardClicked(board: Board) {
    this.store.dispatch(BoardActions.selectBoard({ board }));
  }

  showAddModal() {}

  getsiblingIds(group: BoardGroup, groups: BoardGroup[]): string[] {
    return groups
      .filter((item) => item.id !== group.id)
      .map((item) => item.id.toString());
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
      GroupActions.editBoardGroup({
        boardGroup: {
          ...boardGroup,
          sortOrder,
        },
      })
    );
  }

  createBoard({ name, identifier }: { name: string; identifier: string }) {
    this.store
      .select(selectCurrentProject)
      .pipe(
        first(),
        tap((project) => {
          this.store.dispatch(
            BoardActions.createBoard({
              board: {
                name,
                identifier,
                projectId: project.id,
              },
            })
          );
        })
      )
      .subscribe();
  }

  trackBoardGroup(_: number, group: BoardGroup) {
    return group?.id;
  }
}
