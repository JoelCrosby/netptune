import {
  CdkDrag,
  CdkDragDrop,
  CdkDragHandle,
  CdkDropList,
  moveItemInArray,
} from '@angular/cdk/drag-drop';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  inject,
  linkedSignal,
  OnDestroy,
  viewChild,
} from '@angular/core';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTooltip } from '@angular/material/tooltip';
import { BoardGroupTagsComponent } from '@boards/components/board-group-tags/board-group-tags.component';
import { BoardGroupUsersComponent } from '@boards/components/board-group-users/board-group-users.component';
import { BoardGroupComponent } from '@boards/components/board-group/board-group.component';
import { BoardGroupsFlaggedComponent } from '@boards/components/board-groups-flagged/board-groups-flagged.component';
import { BoardGroupsSearchComponent } from '@boards/components/board-groups-search/board-groups-search.component';
import { BoardGroupsSelectionComponent } from '@boards/components/board-groups-selection/board-groups-selection.component';
import { CreateBoardGroupComponent } from '@boards/components/create-board-group/create-board-group.component';
import { deleteBoard, updateBoard } from '@boards/store/boards/boards.actions';
import {
  clearState,
  deleteBoardGroup,
  editBoardGroup,
  exportBoardTasks,
} from '@boards/store/groups/board-groups.actions';
import {
  selectAllBoardGroupsWithSelection,
  selectBoardGroupsLoaded,
  selectBoardGroupsLoading,
  selectBoardIdentifier,
  selectSelectedBoard,
} from '@boards/store/groups/board-groups.selectors';
import { UpdateBoardGroupRequest } from '@core/models/requests/update-board-group-request';
import { BoardViewGroup } from '@core/models/view-models/board-view';
import { importTasks } from '@core/store/tasks/tasks.actions';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { HeaderAction } from '@core/types/header-action';
import { getNewSortOrder } from '@core/util/sort-order-helper';
import { Store } from '@ngrx/store';
import { InlineEditInputComponent } from '@static/components/inline-edit-input/inline-edit-input.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { ScrollShadowDirective } from '@static/directives/scroll-shadow.directive';

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
  ],
})
export class BoardGroupsViewComponent implements OnDestroy {
  private store = inject(Store);
  private hubService = inject(ProjectTasksHubService);

  readonly importTasksInput =
    viewChild.required<ElementRef>('importTasksInput');

  groups = this.store.selectSignal(selectAllBoardGroupsWithSelection);
  board = this.store.selectSignal(selectSelectedBoard);
  boardName = linkedSignal(() => this.board()?.name);
  loading = this.store.selectSignal(selectBoardGroupsLoading);
  boardGroupsLoaded = this.store.selectSignal(selectBoardGroupsLoaded);

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

  constructor() {
    effect(() => {
      const idSignal = this.store.selectSignal(selectBoardIdentifier);
      const identifier = idSignal();

      if (!identifier) return;

      this.hubService.connect().then(() => {
        this.hubService.addToGroup(identifier);
      });
    });
  }

  ngOnDestroy() {
    this.store.dispatch(clearState());
    void this.hubService.disconnect();
  }

  onTitleSubmitted(title: string) {
    const board = this.board();

    if (!title || !board?.id) return;

    this.store.dispatch(
      updateBoard({
        request: {
          id: board.id,
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

  drop(event: CdkDragDrop<BoardViewGroup[], BoardViewGroup, BoardViewGroup>) {
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
      editBoardGroup({
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
    this.store.dispatch(deleteBoardGroup({ boardGroup }));
  }

  onGroupNameSubmitted(value: Event | string, group: BoardViewGroup) {
    if (value instanceof Event) return;

    const request: UpdateBoardGroupRequest = {
      boardGroupId: group.id,
      name: value,
    };

    this.store.dispatch(editBoardGroup({ request }));
  }

  onImportTasksClicked() {
    const importTasksInput = this.importTasksInput();
    if (!importTasksInput) return;

    importTasksInput.nativeElement.value = null;
    importTasksInput.nativeElement.click();
  }

  onExportTasksClicked() {
    this.store.dispatch(exportBoardTasks());
  }

  onDeleteBoardClicked() {
    const boardId = this.board()?.id;

    if (boardId === undefined || boardId === null) return;

    this.store.dispatch(deleteBoard({ boardId }));
  }

  handleFileInput(event: Event) {
    const files = (event?.target as EventTarget & { files: File[] })?.files;

    if (!files || !files.length) return;

    const file = files[0];

    const boardIdentifier = this.board()?.identifier;

    if (boardIdentifier === undefined || boardIdentifier === null) return;

    this.store.dispatch(importTasks({ boardIdentifier, file }));
  }
}
