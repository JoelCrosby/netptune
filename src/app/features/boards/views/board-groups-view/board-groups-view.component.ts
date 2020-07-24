import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  AfterViewInit,
} from '@angular/core';
import { Board } from '@app/core/models/board';
import { BoardGroup } from '@app/core/models/board-group';
import { selectCurrentProject } from '@core/store/projects/projects.selectors';
import { getNewSortOrder } from '@app/core/util/sort-order-helper';
import * as BoardActions from '@boards/store/boards/boards.actions';
import * as BoardSelectors from '@boards/store/boards/boards.selectors';
import * as GroupActions from '@boards/store/groups/board-groups.actions';
import * as GroupSelectors from '@boards/store/groups/board-groups.selectors';
import { AppState } from '@core/core.state';
import { select, Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { first, map, tap, startWith } from 'rxjs/operators';
import { MatDialog } from '@angular/material/dialog';
import { ConfirmDialogComponent } from '@app/entry/dialogs/confirm-dialog/confirm-dialog.component';
import { TextHelpers } from '@app/core/util/text-helpers';

@Component({
  templateUrl: './board-groups-view.component.html',
  styleUrls: ['./board-groups-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupsViewComponent implements OnInit, AfterViewInit {
  boards$: Observable<Board[]>;
  groups$: Observable<BoardGroup[]>;
  selectedBoard$: Observable<Board>;
  selectedBoardName$: Observable<string>;
  loading$: Observable<boolean>;

  constructor(private store: Store<AppState>, private dialog: MatDialog) {}

  ngOnInit() {
    this.boards$ = this.store.select(BoardSelectors.selectAllBoards);
    this.groups$ = this.store.select(GroupSelectors.selectAllBoardGroups);

    this.selectedBoard$ = this.store.select(BoardSelectors.selectCurrentBoard);
    this.loading$ = this.store
      .select(GroupSelectors.selectBoardGroupsLoading)
      .pipe(startWith(true));

    this.selectedBoardName$ = this.store.pipe(
      select(BoardSelectors.selectCurrentBoard),
      map((board) => board && board.name)
    );
  }

  ngAfterViewInit() {
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

  onDeleteGroupClicked(boardGroup: BoardGroup) {
    this.dialog
      .open(ConfirmDialogComponent, {
        width: '600px',
        data: {
          title: 'Are you sure you want to delete group?',
          content: `Delete group - ${TextHelpers.truncate(boardGroup.name)}`,
          confirm: 'Delete',
        },
      })
      .afterClosed()
      .subscribe((result) => {
        if (result) {
          this.store.dispatch(GroupActions.deleteBoardGroup({ boardGroup }));
        }
      });
  }

  onGroupNameSubmitted(value: any, group: BoardGroup) {
    if (value instanceof Event) return;

    const boardGroup = {
      ...group,
      name: value,
    };

    this.store.dispatch(GroupActions.editBoardGroup({ boardGroup }));
  }
}
