import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnInit,
} from '@angular/core';
import { Board } from '@app/core/models/board';
import { BoardGroup } from '@app/core/models/board-group';
import { getNewSortOrder } from '@app/core/util/sort-order-helper';
import * as GroupActions from '@boards/store/groups/board-groups.actions';
import * as GroupSelectors from '@boards/store/groups/board-groups.selectors';
import { select, Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { map, startWith } from 'rxjs/operators';

@Component({
  templateUrl: './board-groups-view.component.html',
  styleUrls: ['./board-groups-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupsViewComponent implements OnInit, AfterViewInit {
  groups$: Observable<BoardGroup[]>;
  selectedBoard$: Observable<Board>;
  selectedBoardName$: Observable<string>;
  loading$: Observable<boolean>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.groups$ = this.store.select(GroupSelectors.selectAllBoardGroups);
    this.loading$ = this.store
      .select(GroupSelectors.selectBoardGroupsLoading)
      .pipe(startWith(true));

    this.selectedBoardName$ = this.store.pipe(
      select(GroupSelectors.selectBoard),
      map((board) => board && board.name)
    );
  }

  ngAfterViewInit() {
    this.store.dispatch(GroupActions.loadBoardGroups());
  }

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

  trackBoardGroup(_: number, group: BoardGroup) {
    return group?.id;
  }

  onDeleteGroupClicked(boardGroup: BoardGroup) {
    this.store.dispatch(GroupActions.deleteBoardGroup({ boardGroup }));
  }

  onGroupNameSubmitted(value: Event | string, group: BoardGroup) {
    if (value instanceof Event) return;

    const boardGroup: BoardGroup = {
      ...group,
      name: value,
    };

    this.store.dispatch(GroupActions.editBoardGroup({ boardGroup }));
  }
}
