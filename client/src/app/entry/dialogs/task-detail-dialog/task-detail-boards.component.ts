import { httpResource } from '@angular/common/http';
import { Component, ElementRef, computed, inject } from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import { hasPermission } from '@core/auth/has-permission';
import { MAX_PAGE_SIZE } from '@core/models/pagination';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { reloadOnRefresh } from '@core/util/reload-on-refresh';
import { LucideKanban, LucidePlus, LucideX } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-boards',
  imports: [
    DropdownMenuComponent,
    MenuItemComponent,
    IconButtonComponent,
    StrokedButtonComponent,
    TooltipDirective,
    LucideKanban,
    LucidePlus,
    LucideX,
  ],
  template: `
    <div class="mt-4 mb-2 flex items-center justify-between">
      <h4 class="font-sm font-semibold">
        <span i18n="Section heading for the boards a task appears on">
          Boards
        </span>
      </h4>
      @if (canMove() && availableBoards().length) {
        <button
          app-stroked-button
          type="button"
          size="sm"
          (click)="menu.toggle(el.nativeElement)">
          <svg lucidePlus class="h-4 w-4"></svg>
          <span i18n="Button that puts this task on another board">
            Add to board
          </span>
        </button>
      }
    </div>

    <app-dropdown-menu #menu>
      <div class="min-w-52">
        @for (board of availableBoards(); track board.id) {
          <button app-menu-item (click)="addToBoard(board.id); menu.close()">
            <svg lucideKanban class="h-4 w-4 shrink-0"></svg>
            <span class="flex-1 truncate text-left">{{ board.name }}</span>
          </button>
        }
      </div>
    </app-dropdown-menu>

    <ul class="flex flex-col gap-1">
      @for (placement of placements(); track placement.boardId) {
        <li
          class="border-border bg-card flex items-center gap-3 rounded border px-3 py-2">
          <svg lucideKanban class="text-muted h-4 w-4 shrink-0"></svg>

          <span class="flex-1 truncate text-sm">{{ placement.boardName }}</span>

          <span class="text-muted shrink-0 text-xs">
            {{ placement.boardGroupName }}
          </span>

          @if (canMove() && placements().length > 1) {
            <button
              app-icon-button
              i18n-appTooltip="
                Tooltip on the button that takes a task off a board
              "
              appTooltip="Remove from board"
              i18n-aria-label="
                Accessible label for the button that takes a task off a board
              "
              aria-label="Remove from board"
              (click)="removeFromBoard(placement.boardId)">
              <svg lucideX class="h-4 w-4"></svg>
            </button>
          }
        </li>
      } @empty {
        <div class="text-muted flex items-center gap-2 text-sm">
          <svg lucideKanban class="h-4 w-4"></svg>
          <span i18n="Empty state when a task is not on any board">
            Not on any board
          </span>
        </div>
      }
    </ul>
  `,
})
export class TaskDetailBoardsComponent {
  readonly el = inject(ElementRef);

  private readonly taskDetail = inject(TaskDetailService);
  private readonly taskCommands = inject(TaskCommandsService);

  readonly task = this.taskDetail.task;
  readonly canMove = hasPermission(PERMISSIONS.tasks.move);

  readonly placements = computed(() => this.task()?.placements ?? []);

  private readonly boards = httpResource<BoardViewModel[]>(
    () => {
      const projectId = this.task()?.projectId;

      if (!projectId) return undefined;

      return {
        url: `api/boards/project/${projectId}`,
        params: { page: 1, pageSize: MAX_PAGE_SIZE },
      };
    },
    { defaultValue: [] }
  );

  readonly availableBoards = computed(() => {
    const placedBoardIds = new Set(
      this.placements().map((placement) => placement.boardId)
    );

    return this.boards.value().filter((board) => !placedBoardIds.has(board.id));
  });

  constructor() {
    reloadOnRefresh(this.boards, ['boards']);
  }

  addToBoard(boardId: number) {
    const task = this.task();

    if (!task) return;

    this.taskCommands.addToBoard(
      task.id,
      { boardId },
      { onPlaced: () => this.taskDetail.reload() }
    );
  }

  removeFromBoard(boardId: number) {
    const task = this.task();

    if (!task) return;

    this.taskCommands.removeFromBoard(task.id, boardId, {
      onRemoved: () => this.taskDetail.reload(),
    });
  }
}
