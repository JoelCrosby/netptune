import {
  CdkDragDrop,
  moveItemInArray,
  CdkDropList,
  CdkDrag,
  CdkDragHandle,
} from '@angular/cdk/drag-drop';
import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import * as BoardActions from '@boards/store//boards/boards.actions';
import * as GroupActions from '@boards/store/groups/board-groups.actions';
import * as GroupSelectors from '@boards/store/groups/board-groups.selectors';
import { Board } from '@core/models/board';
import { UpdateBoardGroupRequest } from '@core/models/requests/update-board-group-request';
import { BoardViewGroup } from '@core/models/view-models/board-view';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { HeaderAction } from '@core/types/header-action';
import { getNewSortOrder } from '@core/util/sort-order-helper';
import { select, Store } from '@ngrx/store';
import { from, Observable, of } from 'rxjs';
import { filter, first, map, startWith, switchMap, tap } from 'rxjs/operators';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { AsyncPipe } from '@angular/common';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { BoardGroupsSearchComponent } from '@boards/components/board-groups-search/board-groups-search.component';
import { BoardGroupUsersComponent } from '@boards/components/board-group-users/board-group-users.component';
import { BoardGroupTagsComponent } from '@boards/components/board-group-tags/board-group-tags.component';
import { BoardGroupsFlaggedComponent } from '@boards/components/board-groups-flagged/board-groups-flagged.component';
import { BoardGroupsSelectionComponent } from '@boards/components/board-groups-selection/board-groups-selection.component';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { ScrollShadowDirective } from '@static/directives/scroll-shadow.directive';
import { BoardGroupComponent } from '@boards/components/board-group/board-group.component';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';
import { InlineEditInputComponent } from '@static/components/inline-edit-input/inline-edit-input.component';
import { MatIconButton } from '@angular/material/button';
import { CreateBoardGroupComponent } from '@boards/components/create-board-group/create-board-group.component';

@Component({
  templateUrl: './board-groups-view.component.html',
  styleUrls: ['./board-groups-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [],
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    BoardGroupsSearchComponent,
    BoardGroupUsersComponent,
    BoardGroupTagsComponent,
    BoardGroupsFlaggedComponent,
    BoardGroupsSelectionComponent,
    MatProgressSpinner,
    CdkDropList,
    ScrollShadowDirective,
    BoardGroupComponent,
    CdkDrag,
    CdkDragHandle,
    MatIcon,
    MatTooltip,
    InlineEditInputComponent,
    MatIconButton,
    CreateBoardGroupComponent,
    AsyncPipe
],
})
export class BoardGroupsViewComponent
  implements OnInit, OnDestroy, AfterViewInit
{
  private store = inject(Store);
  private hubService = inject(ProjectTasksHubService);

  @ViewChild('importTasksInput') importTasksInput!: ElementRef;

  groups$!: Observable<BoardViewGroup[]>;
  selectedBoard$!: Observable<Board | undefined>;
  selectedBoardName$!: Observable<string | undefined>;
  loading$!: Observable<boolean>;
  boardGroupsLoaded$!: Observable<boolean>;

  secondaryActions: HeaderAction[] = [
    {
      label: 'Import Tasks',
      click: () => this.onImportTasksClicked(),
      icon: 'file_upload',
      iconClass: 'material-icons-outlined',
    },
    {
      label: 'Export Board Tasks',
      click: () => this.onExportTasksClicked(),
      icon: 'file_download',
      iconClass: 'material-icons-outlined',
    },
    {
      label: 'Delete Board',
      click: () => this.onDeleteBoardClicked(),
      icon: 'delete',
      iconClass: 'material-icons-outlined',
    },
  ];

  private board?: Board;

  ngOnInit() {
    this.groups$ = this.store.select(
      GroupSelectors.selectAllBoardGroupsWithSelection
    );

    this.loading$ = this.store
      .select(GroupSelectors.selectBoardGroupsLoading)
      .pipe(startWith(true));

    this.boardGroupsLoaded$ = this.store
      .select(GroupSelectors.selectBoardGroupsLoaded)
      .pipe(startWith(true));

    this.selectedBoardName$ = this.store.pipe(
      select(GroupSelectors.selectBoard),
      filter((board) => !!board),
      tap((board) => {
        this.board = board;
      }),
      map((board) => board && board.name)
    );

    this.store
      .select(GroupSelectors.selectBoardIdentifier)
      .pipe(
        filter((val) => !!val),
        first(),
        switchMap((identifier) =>
          from(this.hubService.connect()).pipe(
            switchMap(() => {
              if (!identifier) return of({ type: 'NOOP' });

              return this.hubService.addToGroup(identifier);
            })
          )
        )
      )
      .subscribe();
  }

  ngAfterViewInit() {
    this.store.dispatch(GroupActions.loadBoardGroups());
  }

  ngOnDestroy() {
    this.store.dispatch(GroupActions.clearState());
    void this.hubService.disconnect();
  }

  onTitleSubmitted(title: string) {
    if (!title || !this.board?.id) return;

    this.store.dispatch(
      BoardActions.updateBoard({
        request: {
          id: this.board.id,
          name: title,
        },
      })
    );
  }

  getsiblingIds(group: BoardViewGroup, groups: BoardViewGroup[]): string[] {
    return groups
      .filter((item) => item.id !== group.id)
      .map((item) => item.id.toString());
  }

  drop(event: CdkDragDrop<BoardViewGroup[]>) {
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

    this.moveBoardGroup(data as BoardViewGroup, order);
  }

  moveBoardGroup(boardGroup: BoardViewGroup, sortOrder: number) {
    this.store.dispatch(
      GroupActions.editBoardGroup({
        request: {
          boardGroupId: boardGroup.id,
          sortOrder,
        },
      })
    );
  }

  trackBoardGroup(_: number, group: BoardViewGroup) {
    return group?.id;
  }

  onDeleteGroupClicked(boardGroup: BoardViewGroup) {
    this.store.dispatch(GroupActions.deleteBoardGroup({ boardGroup }));
  }

  onGroupNameSubmitted(value: Event | string, group: BoardViewGroup) {
    if (value instanceof Event) return;

    const request: UpdateBoardGroupRequest = {
      boardGroupId: group.id,
      name: value,
    };

    this.store.dispatch(GroupActions.editBoardGroup({ request }));
  }

  onImportTasksClicked() {
    if (!this.importTasksInput) return;

    this.importTasksInput.nativeElement.value = null;
    this.importTasksInput.nativeElement.click();
  }

  onExportTasksClicked() {
    this.store.dispatch(GroupActions.exportBoardTasks());
  }

  onDeleteBoardClicked() {
    const boardId = this.board?.id;

    if (boardId === undefined || boardId === null) return;

    this.store.dispatch(BoardActions.deleteBoard({ boardId }));
  }

  handleFileInput(event: Event) {
    const files = (event?.target as EventTarget & { files: File[] })?.files;

    if (!files || !files.length) return;

    const file = files[0];

    const boardIdentifier = this.board?.identifier;

    if (boardIdentifier === undefined || boardIdentifier === null) return;

    this.store.dispatch(TaskActions.importTasks({ boardIdentifier, file }));
  }
}
