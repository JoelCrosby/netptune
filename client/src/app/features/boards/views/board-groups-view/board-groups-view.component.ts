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
  input,
  linkedSignal,
  OnDestroy,
} from '@angular/core';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { BoardCommandsService } from '@core/services/board-commands.service';
import { BoardGroupCommandsService } from '@core/services/board-group-commands.service';
import {
  BoardSelectionService,
  BoardViewGroupWithSelection,
} from '@core/services/board-selection.service';
import { BoardViewService } from '@core/services/board-view.service';
import { BoardGroupHeaderComponent } from '@boards/components/board-group-header/board-group-header.component';
import { BoardGroupStatusDotComponent } from '@boards/components/board-group-status-dot/board-group-status-dot.component';
import { BoardGroupComponent } from '@boards/components/board-group/board-group.component';
import { CreateBoardComponent } from '@boards/components/create-board/create-board.component';
import { CreateBoardGroupComponent } from '@boards/components/create-board-group/create-board-group.component';
import { BoardGroupDialogComponent } from '@entry/dialogs/board-group-dialog/board-group-dialog.component';
import { UpdateBoardGroupRequest } from '@core/models/requests/update-board-group-request';
import { BoardViewGroup } from '@core/models/view-models/board-view';
import {
  BOARDS_HIDDEN_GROUP_IDS,
  BOARDS_TASK_SORT,
} from '@core/models/user-preferences';
import { DialogService } from '@core/services/dialog.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { statusResource } from '@core/resources/status.resources';
import { ManageBoardGroupsDialogComponent } from '@boards/components/manage-board-groups-dialog/manage-board-groups-dialog.component';
import { hiddenGroupIdsForBoard } from '@boards/util/hidden-board-groups';
import {
  boardTaskSortForBoard,
  sortBoardViewTasks,
} from '@boards/util/board-task-sort';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { HeaderAction } from '@core/types/header-action';
import { getNewSortOrder } from '@core/util/sort-order-helper';
import {
  LucideDelete,
  LucideEllipsisVertical,
  LucideEyeOff,
  LucideFileDown,
  LucideFileUp,
  LucideSettings2,
  LucideX,
} from '@lucide/angular';
import { Router } from '@angular/router';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { delayedLoading } from '@core/util/delayed-loading';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { InlineEditInputComponent } from '@static/components/inline-edit-input/inline-edit-input.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { SkeletonBoardComponent } from '@static/components/skeleton/skeleton-board.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { ScrollShadowDirective } from '@static/directives/scroll-shadow.directive';

@Component({
  selector: 'app-board-groups-view',
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
    SkeletonBoardComponent,
    PageContainerComponent,
    PageHeaderComponent,
    BoardGroupHeaderComponent,
    CdkDropList,
    ScrollShadowDirective,
    BoardGroupComponent,
    CdkDrag,
    CdkDragHandle,
    LucideEllipsisVertical,
    LucideX,
    BoardGroupStatusDotComponent,
    InlineEditInputComponent,
    IconButtonComponent,
    CreateBoardGroupComponent,
  ],
  template: `
    <app-page-container
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
        @if (showSkeleton()) {
          <app-skeleton-board />
        }
      } @else {
        @if (visibleGroups(); as groups) {
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
                  group.statusId
                    ? (statusMap().get(group.statusId) ?? null)
                    : null
                "
                [siblingIds]="siblingIdMap().get(group.id) ?? []"
                [reorderDisabled]="reorderDisabled()"
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
                      <app-board-group-status-dot [status]="status" />
                    }
                    <app-inline-edit-input
                      class="hover:bg-primary/6 ml-2 w-full rounded px-1.5 py-1 transition-colors duration-200"
                      [size]="group.name.length"
                      [value]="group.name"
                      [disabled]="!isAuthenticated()"
                      (submitted)="
                        onGroupNameSubmitted($event, group)
                      "></app-inline-edit-input>
                    <span class="text-foreground/30 ml-[.2rem] font-bold">
                      {{ group.tasks.length }}
                    </span>
                  </div>
                  @if (isAuthenticated()) {
                    <button
                      app-icon-button
                      i18n-title="
                        Tooltip on the button that edits a board group
                      "
                      title="Edit group"
                      class="invisible mx-[.2rem] group-hover/header:visible"
                      (click)="onEditGroupClicked(group)">
                      <svg
                        lucideEllipsisVertical
                        class="text-foreground/40 h-4 w-4"></svg>
                    </button>
                    <button
                      app-icon-button
                      i18n-title="
                        Tooltip on the button that deletes a board group
                      "
                      title="Delete group"
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
    </app-page-container>
  `,
})
export class BoardGroupsViewComponent implements OnDestroy {
  private store = inject(Store);
  private boardCommands = inject(BoardCommandsService);
  private boardView = inject(BoardViewService);
  private selection = inject(BoardSelectionService);
  private boardGroupCommands = inject(BoardGroupCommandsService);
  private hubService = inject(ProjectTasksHubService);
  private dialog = inject(DialogService);
  private preferences = inject(UserPreferencesService);
  private router = inject(Router);

  private workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  isAuthenticated = this.store.selectSignal(selectIsAuthenticated);

  groups = this.selection.groups;

  hiddenGroupIds = computed(() => {
    const boardId = this.board()?.id;

    if (boardId === undefined) return new Set<number>();

    const value = this.preferences.effectiveValueFor(BOARDS_HIDDEN_GROUP_IDS);

    return new Set(hiddenGroupIdsForBoard(value, boardId));
  });

  taskSort = computed(() => {
    const boardId = this.board()?.id;

    if (boardId === undefined) return boardTaskSortForBoard(undefined, 0);

    return boardTaskSortForBoard(
      this.preferences.effectiveValueFor(BOARDS_TASK_SORT),
      boardId
    );
  });

  reorderDisabled = computed(() => this.taskSort().field !== 'custom');

  visibleGroups = computed(() => {
    const hidden = this.hiddenGroupIds();
    const sort = this.taskSort();

    return this.groups()
      .filter((group) => !hidden.has(group.id))
      .map((group) => ({
        ...group,
        tasks: sortBoardViewTasks(group.tasks, sort),
      }));
  });

  statuses = statusResource();
  statusMap = computed(() => {
    return new Map(this.statuses.value().map((status) => [status.id, status]));
  });

  readonly id = input.required<string>();

  board = this.boardView.board;
  boardName = linkedSignal(() => this.board()?.name);
  loading = this.boardView.loading;
  showSkeleton = delayedLoading(this.loading);
  boardGroupsLoaded = this.boardView.loaded;

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
      label: $localize`:Overflow action that edits the board:Edit Board`,
      click: () => this.onEditBoardClicked(),
      icon: LucideSettings2,
    },
    {
      label: $localize`:Overflow action that opens the manage-groups dialog:Manage Groups`,
      click: () => this.onManageGroupsClicked(),
      icon: LucideEyeOff,
    },
    {
      label: $localize`:Overflow action that opens the CSV task import dialog:Import Tasks`,
      click: () => this.onImportTasksClicked(),
      icon: LucideFileUp,
    },
    {
      label: $localize`:Overflow action that downloads the board tasks as CSV:Export Board Tasks`,
      click: () => this.onExportTasksClicked(),
      icon: LucideFileDown,
    },
    {
      label: $localize`:Overflow action that deletes the board:Delete Board`,
      click: () => this.onDeleteBoardClicked(),
      icon: LucideDelete,
    },
  ];

  constructor() {
    // Navigating between boards reuses this component, so the open board has to
    // follow the input rather than be claimed once on the way in.
    effect(() => {
      const identifier = this.id();

      this.boardView.open(identifier);
      this.hubService.addToGroup(identifier);
    });
  }

  ngOnDestroy() {
    this.boardView.close();
    this.hubService.leaveGroup();
  }

  onTitleSubmitted(title: string) {
    const board = this.board();

    if (!title || !board?.id) return;

    this.boardCommands.update({ id: board.id, name: title });
  }

  getsiblingIds(group: BoardViewGroup, groups: BoardViewGroup[]): string[] {
    return groups
      .filter((item) => item.id !== group.id)
      .map((item) => item.id.toString());
  }

  drop(
    event: CdkDragDrop<
      BoardViewGroupWithSelection[],
      BoardViewGroupWithSelection,
      BoardViewGroupWithSelection
    >
  ) {
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
    this.boardGroupCommands.editGroup({
      boardGroupId: boardGroup.id,
      sortOrder,
    });
  }

  trackBoardGroup(_: number, group: BoardViewGroup) {
    return group?.id;
  }

  onDeleteGroupClicked(boardGroup: BoardViewGroup) {
    this.boardGroupCommands.deleteGroup(boardGroup);
  }

  onEditGroupClicked(group: BoardViewGroup) {
    this.dialog.open(BoardGroupDialogComponent, {
      width: '600px',
      data: {
        boardId: group.boardId,
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

    this.boardGroupCommands.editGroup(request);
  }

  onImportTasksClicked() {
    const boardIdentifier = this.board()?.identifier;

    if (boardIdentifier === undefined || boardIdentifier === null) return;

    this.router.navigate(
      ['/', this.workspaceId(), 'settings', 'workspace', 'data', 'import'],
      { queryParams: { board: boardIdentifier } }
    );
  }

  onEditBoardClicked() {
    const board = this.board();

    if (!board) return;

    this.dialog.open(CreateBoardComponent, {
      width: '600px',
      data: board,
    });
  }

  onManageGroupsClicked() {
    this.dialog.open(ManageBoardGroupsDialogComponent, {
      width: '600px',
    });
  }

  onExportTasksClicked() {
    this.boardGroupCommands.exportTasks();
  }

  onDeleteBoardClicked() {
    const boardId = this.board()?.id;

    if (boardId === undefined || boardId === null) return;

    this.boardCommands.delete(boardId);
  }
}
