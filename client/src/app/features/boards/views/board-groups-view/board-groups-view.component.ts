import {
  CdkDrag,
  CdkDragDrop,
  CdkDragHandle,
  CdkDropList,
  moveItemInArray,
} from '@angular/cdk/drag-drop';
import {
  Component,
  computed,
  effect,
  inject,
  linkedSignal,
  OnDestroy,
} from '@angular/core';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import {
  deleteBoard,
  updateBoard,
} from '@app/core/store/boards/boards.actions';
import {
  clearState,
  deleteBoardGroup,
  editBoardGroup,
  exportBoardTasks,
} from '@app/core/store/groups/board-groups.actions';
import {
  selectAllBoardGroupsWithSelection,
  selectBoardGroupsLoaded,
  selectBoardGroupsLoading,
  selectBoardIdentifier,
  selectSelectedBoard,
} from '@app/core/store/groups/board-groups.selectors';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { BoardGroupHeaderComponent } from '@boards/components/board-group-header/board-group-header.component';
import { BoardGroupComponent } from '@boards/components/board-group/board-group.component';
import { CreateBoardComponent } from '@boards/components/create-board/create-board.component';
import { CreateBoardGroupComponent } from '@boards/components/create-board-group/create-board-group.component';
import { ImportTasksDialogComponent } from '@boards/components/import-tasks-dialog/import-tasks-dialog.component';
import { BoardGroupDialogComponent } from '@entry/dialogs/board-group-dialog/board-group-dialog.component';
import { UpdateBoardGroupRequest } from '@core/models/requests/update-board-group-request';
import { BoardViewGroup } from '@core/models/view-models/board-view';
import { DialogService } from '@core/services/dialog.service';
import { statusResource } from '@core/resources/status.resources';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { HeaderAction } from '@core/types/header-action';
import { getNewSortOrder } from '@core/util/sort-order-helper';
import {
  LucideDelete,
  LucideFileDown,
  LucideFileUp,
  LucidePencil,
  LucideX,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { InlineEditInputComponent } from '@static/components/inline-edit-input/inline-edit-input.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { ScrollShadowDirective } from '@static/directives/scroll-shadow.directive';

@Component({
  styles: [
    `
      .cdk-drag-placeholder {
        opacity: 0;
      }
      .cdk-drag-animating {
        transition: transform 0.25s cubic-bezier(0, 0, 0.2, 1);
      }
      .cdk-drag-preview {
        box-shadow:
          0 5px 5px -3px rgba(0, 0, 0, 0.2),
          0 8px 10px 1px rgba(0, 0, 0, 0.14),
          0 3px 14px 2px rgba(0, 0, 0, 0.12);
        transition: box-shadow 0.3s cubic-bezier(0, 0, 0.2, 1);
      }
      .board-groups.cdk-drop-list-dragging
        .board-group:not(.cdk-drag-placeholder) {
        transition: transform 0.5s cubic-bezier(0, 0, 0.2, 1);
      }
    `,
  ],
  providers: [],
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    BoardGroupHeaderComponent,
    SpinnerComponent,
    CdkDropList,
    ScrollShadowDirective,
    BoardGroupComponent,
    CdkDrag,
    CdkDragHandle,
    LucidePencil,
    LucideX,
    TooltipDirective,
    InlineEditInputComponent,
    IconButtonComponent,
    CreateBoardGroupComponent,
  ],
  template: `<app-page-container
    [marginBottom]="false"
    [verticalPadding]="false"
    [fullHeight]="true"
    [centerPage]="false">
    @if (boardGroupsLoaded()) {
      <app-page-header
        [title]="boardName()"
        [titleEditable]="isAuthenticated()"
        [overflowActions]="isAuthenticated() ? secondaryActions : []"
        (titleSubmitted)="onTitleSubmitted($event)">
        <div class="flex flex-wrap items-center gap-3">
          <app-board-group-header />
        </div>
      </app-page-header>
    }

    @if (loading()) {
      <div class="flex h-full flex-col items-center justify-center">
        <app-spinner diameter="32px" />
      </div>
    } @else {
      @if (groups(); as groups) {
        <div
          cdkDropList
          appScrollShadow
          class="board-groups custom-scroll flex max-h-[calc(100vh-180px)] w-full flex-1 flex-row overflow-hidden overflow-x-scroll rounded-lg pb-4 max-[600px]:max-h-[calc(100vh-154px)]"
          cdkDropListOrientation="horizontal"
          (cdkDropListDropped)="drop($event)"
          [cdkDropListData]="groups">
          @for (group of groups; track trackBoardGroup($index, group)) {
            <app-board-group
              cdkDrag
              [cdkDragDisabled]="!isAuthenticated()"
              class="board-group mr-4 flex w-75 flex-none flex-col overflow-hidden rounded-[.4rem]"
              [cdkDragData]="group"
              [group]="group"
              [assignedStatus]="
                group.statusId ? (statusMap().get(group.statusId) ?? null) : null
              "
              [siblingIds]="siblingIdMap().get(group.id) ?? []"
              [dragListId]="group.id.toString()">
              <span
                cdkDragHandle
                class="group/header flex cursor-pointer flex-row items-center justify-between uppercase">
                <div
                  class="text-foreground/60 flex h-12.5 w-full flex-row-reverse items-center justify-end pl-4 text-sm font-medium tracking-[.1px]">
                  @if (
                    group.statusId && statusMap().get(group.statusId);
                    as status
                  ) {
                    <span
                      class="ml-[.4rem] h-2.5 w-2.5 flex-none rounded-full"
                      [style.background-color]="status.color || 'var(--primary)'"
                      [appTooltip]="
                        'Tasks moved into this group will be set to ' +
                        status.name
                      "></span>
                  }
                  <app-inline-edit-input
                    class="hover:bg-primary/6 ml-2 w-full rounded px-1.5 py-1 transition-colors duration-200"
                    [size]="group.name.length"
                    [value]="group.name"
                    [disabled]="!isAuthenticated()"
                    (submitted)="onGroupNameSubmitted($event, group)">
                  </app-inline-edit-input>
                  <span class="text-foreground/30 ml-[.2rem] font-bold">{{
                    group.tasks.length
                  }}</span>
                </div>
                @if (isAuthenticated()) {
                  <button
                    app-icon-button
                    class="invisible mx-[.2rem] group-hover/header:visible"
                    (click)="onEditGroupClicked(group)">
                    <svg lucidePencil class="text-foreground/40 h-4 w-4"></svg>
                  </button>
                  <button
                    app-icon-button
                    class="invisible mx-[.2rem] group-hover/header:visible"
                    (click)="onDeleteGroupClicked(group)">
                    <svg lucideX class="text-foreground/40 h-4 w-4"></svg>
                  </button>
                }
              </span>
            </app-board-group>
          }
          @if (isAuthenticated()) {
            <app-create-board-group
              class="board-group mr-4 flex w-75 flex-none flex-col overflow-hidden rounded-[.4rem]" />
          }
        </div>
      }
    }
  </app-page-container> `,
})
export class BoardGroupsViewComponent implements OnDestroy {
  private store = inject(Store);
  private hubService = inject(ProjectTasksHubService);
  private dialog = inject(DialogService);

  isAuthenticated = this.store.selectSignal(selectIsAuthenticated);

  groups = this.store.selectSignal(selectAllBoardGroupsWithSelection);
  statuses = statusResource();
  statusMap = computed(
    () => new Map(this.statuses.value().map((status) => [status.id, status]))
  );
  board = this.store.selectSignal(selectSelectedBoard);
  boardName = linkedSignal(() => this.board()?.name);
  loading = this.store.selectSignal(selectBoardGroupsLoading);
  boardGroupsLoaded = this.store.selectSignal(selectBoardGroupsLoaded);
  boardIdentifier = this.store.selectSignal(selectBoardIdentifier);

  siblingIdMap = computed(() => {
    const groups = this.groups();
    return new Map(
      groups.map((g, _, arr) => [
        g.id,
        arr.filter((s) => s.id !== g.id).map((s) => s.id.toString()),
      ])
    );
  });

  secondaryActions: HeaderAction[] = [
    {
      label: 'Edit Board',
      click: () => this.onEditBoardClicked(),
      icon: LucidePencil,
    },
    {
      label: 'Import Tasks',
      click: () => this.onImportTasksClicked(),
      icon: LucideFileUp,
    },
    {
      label: 'Export Board Tasks',
      click: () => this.onExportTasksClicked(),
      icon: LucideFileDown,
    },
    {
      label: 'Delete Board',
      click: () => this.onDeleteBoardClicked(),
      icon: LucideDelete,
    },
  ];

  constructor() {
    effect(() => {
      const identifier = this.boardIdentifier();

      if (!identifier) return;

      this.hubService.addToGroup(identifier);
    });
  }

  ngOnDestroy() {
    this.store.dispatch(clearState());
    this.hubService.disconnect();
  }

  onTitleSubmitted(title: string) {
    const board = this.board();

    if (!title || !board?.id) return;

    this.store.dispatch(
      updateBoard.init({
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
      editBoardGroup.init({
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
    this.store.dispatch(deleteBoardGroup.init({ boardGroup }));
  }

  onEditGroupClicked(group: BoardViewGroup) {
    this.dialog.open(BoardGroupDialogComponent, {
      width: '600px',
      data: {
        boardId: group.boardId,
        identifier: this.boardIdentifier(),
        boardGroupId: group.id,
        name: group.name,
        statusId: group.statusId,
      },
    });
  }

  onGroupNameSubmitted(value: Event | string, group: BoardViewGroup) {
    if (value instanceof Event) return;

    const request: UpdateBoardGroupRequest = {
      boardGroupId: group.id,
      name: value,
    };

    this.store.dispatch(editBoardGroup.init({ request }));
  }

  onImportTasksClicked() {
    const boardIdentifier = this.board()?.identifier;

    if (boardIdentifier === undefined || boardIdentifier === null) return;

    this.dialog.open(ImportTasksDialogComponent, {
      panelClass: 'app-modal-class',
      data: {
        boardIdentifier,
      },
    });
  }

  onEditBoardClicked() {
    const board = this.board();

    if (!board) return;

    this.dialog.open(CreateBoardComponent, {
      width: '600px',
      data: board,
    });
  }

  onExportTasksClicked() {
    this.store.dispatch(exportBoardTasks.init());
  }

  onDeleteBoardClicked() {
    const boardId = this.board()?.id;

    if (boardId === undefined || boardId === null) return;

    this.store.dispatch(deleteBoard.init({ boardId }));
  }
}
