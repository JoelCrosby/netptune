import { CdkDrag, CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import {
  AfterViewInit,
  Component,
  computed,
  DestroyRef,
  ElementRef,
  inject,
  input,
  OnDestroy,
  signal,
  viewChild,
} from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { BoardGroupCommandsService } from '@core/services/board-group-commands.service';
import { BoardBackgroundService } from '@core/services/board-background.service';
import { BoardComposerService } from '@core/services/board-composer.service';
import { BoardSelectionService } from '@core/services/board-selection.service';
import { BoardViewService } from '@core/services/board-view.service';
import { mouseMoveHandler } from '@boards/util/mouse-move-handler';
import { Selected } from '@core/models/selected';
import { Status } from '@core/models/status';
import {
  BoardViewGroup,
  BoardViewTask,
} from '@core/models/view-models/board-view';
import { DialogService } from '@core/services/dialog.service';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { LucideKanban, LucideTrash2 } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { ScrollShadowVericalDirective } from '@static/directives/scroll-shadow-vertical.directive';
import { fromEvent } from 'rxjs';
import { BoardGroupCardComponent } from '../board-group-card/board-group-card.component';
import { BoardGroupTaskInlineComponent } from '../board-group-task-inline/board-group-task-inline.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';

@Component({
  selector: 'app-board-group',
  styles: `
    .board-group-surface.translucent {
      background-color: rgba(var(--board-group-rgb), 0.82);
      -webkit-backdrop-filter: blur(14px);
      backdrop-filter: blur(14px);
    }

    .cdk-drag-placeholder {
      opacity: 0.2;
    }

    .cdk-drag-preview .netp-card {
      box-shadow:
        0 5px 5px -3px rgba(0, 0, 0, 0.2),
        0 8px 10px 1px rgba(0, 0, 0, 0.14),
        0 3px 14px 2px rgba(0, 0, 0, 0.12) !important;
    }

    .cdk-drag-animating {
      transition: transform 0.25s cubic-bezier(0, 0, 0.2, 1);
    }

    .board-task-list.cdk-drop-list-dragging
      .board-group-task-card:not(.cdk-drag-placeholder) {
      transition: transform 140ms cubic-bezier(0, 0, 0.2, 1);
    }
  `,
  imports: [
    CdkDropList,
    ScrollShadowVericalDirective,
    BoardGroupCardComponent,
    CdkDrag,
    BoardGroupTaskInlineComponent,
    StrokedButtonComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    LucideKanban,
    LucideTrash2,
  ],
  template: `
    <div
      class="board-group-surface border-border bg-board-group relative flex h-full flex-1 flex-col rounded border"
      [class.translucent]="hasBoardBackground()">
      <ng-content />

      <div #container class="min-h-0 flex-1">
        <div
          cdkDropList
          appScrollShadowVertical
          class="custom-scroll board-task-list flex h-full flex-col overflow-y-auto p-[.6rem]"
          [id]="dragListId()"
          [cdkDropListConnectedTo]="siblingIds()"
          (cdkDropListDropped)="drop($event)"
          [cdkDropListData]="group().tasks">
          @for (task of group().tasks; track trackGroupTask($index, task)) {
            <app-board-group-card
              cdkDrag
              [cdkDragDisabled]="dragDisabled()"
              class="board-group-task-card cursor-pointer"
              [class.cursor-default!]="dragDisabled()"
              [cdkDragData]="task"
              [task]="task"
              [groupId]="group().id"
              (cdkDragStarted)="onDragStarted()"
              (cdkDragReleased)="onDragRelease()"
              (contextmenu)="onTaskContextMenu($event, task)"
              (click)="
                onTaskClicked($event, task, group().id)
              "></app-board-group-card>
          }

          @if (isInlineActive()) {
            <app-board-group-task-inline
              (canceled)="onInlineCanceled()"
              [boardGroupId]="group().id"></app-board-group-task-inline>
          }

          @if (showAddButton()) {
            <div class="h-11.5 p-[.3rem]">
              <button
                app-stroked-button
                color="primary"
                class="block w-full"
                (click)="onAddTaskClicked()">
                <span i18n="Button that adds a task to this board group">
                  CREATE TASK
                </span>
              </button>
            </div>
          } @else {
            <div class="h-11.5 min-h-11.5 w-full">{{ ' ' }}</div>
          }
        </div>
      </div>
    </div>

    <app-dropdown-menu #taskMenu [push]="true">
      <div class="min-w-52">
        @if (canMove()) {
          <button
            app-menu-item
            type="button"
            (click)="onRemoveFromBoardClicked()">
            <svg lucideKanban class="h-4 w-4 shrink-0"></svg>
            <span i18n="Menu item that takes a task off the board being viewed"
              >Remove from board</span
            >
          </button>
        }

        @if (canDelete()) {
          @if (canMove()) {
            <div class="border-border/50 my-1 border-t"></div>
          }

          <button
            app-menu-item
            type="button"
            class="text-warn!"
            (click)="onDeleteTaskClicked()">
            <svg lucideTrash2 class="h-4 w-4 shrink-0"></svg>
            <span i18n="Menu item that deletes a task">Delete task</span>
          </button>
        }
      </div>
    </app-dropdown-menu>
  `,
})
export class BoardGroupComponent implements OnDestroy, AfterViewInit {
  private boardView = inject(BoardViewService);
  private selection = inject(BoardSelectionService);
  private composer = inject(BoardComposerService);
  private boardCommands = inject(BoardGroupCommandsService);
  private dialog = inject(DialogService);
  private boardBackground = inject(BoardBackgroundService);
  private taskCommands = inject(TaskCommandsService);
  private destroyRef = inject(DestroyRef);

  readonly canMove = hasPermission(PERMISSIONS.tasks.move);
  readonly canDelete = hasPermission(PERMISSIONS.tasks.delete);

  readonly hasBoardBackground = computed(() => {
    return this.boardBackground.imageUrl() !== null;
  });

  readonly dragListId = input.required<string>();
  readonly group = input.required<BoardViewGroup>();
  readonly assignedStatus = input<Status | null>(null);
  readonly siblingIds = input.required<string[]>();
  readonly reorderDisabled = input(false);

  readonly container = viewChild.required<ElementRef>('container');
  private readonly taskMenu =
    viewChild.required<DropdownMenuComponent>('taskMenu');

  private readonly menuTask = signal<BoardViewTask | null>(null);

  readonly dragDisabled = computed(() => {
    return !this.isAuthenticated() || this.reorderDisabled();
  });

  focused = signal(false);
  isAuthenticated = inject(SessionService).isAuthenticated;
  isDragging = this.boardView.isDragging;
  private inlineActiveGroupId = this.composer.activeGroupId;
  isInlineActive = computed(
    () => this.group().id === this.inlineActiveGroupId()
  );

  showAddButton = computed(() => {
    return (
      this.isAuthenticated() &&
      this.focused() &&
      !this.isDragging() &&
      !this.isInlineActive()
    );
  });

  ngAfterViewInit() {
    const el: HTMLDivElement = this.container().nativeElement;

    fromEvent(el, 'mouseenter', { passive: true })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: () => this.focused.set(true) });

    fromEvent(el, 'mouseleave', { passive: true })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: () => this.focused.set(false) });
  }

  ngOnDestroy() {
    document.removeEventListener('mousemove', mouseMoveHandler);
  }

  onAddTaskClicked() {
    this.composer.open(this.group().id);
  }

  onInlineCanceled() {
    this.composer.close();
  }

  drop(event: CdkDragDrop<BoardViewTask[]>) {
    const { data: task } = event.item;

    const isTransfer = event.container.id !== event.previousContainer.id;

    this.boardCommands.moveTask(
      {
        newGroupId: +event.container.id,
        oldGroupId: +event.previousContainer.id,
        taskId: task.id,
        currentIndex: event.currentIndex,
        previousIndex: event.previousIndex,
      },
      // Only a transfer between groups applies the target group's status.
      isTransfer ? this.assignedStatus() : null
    );
  }

  onDragStarted() {
    this.trackMousePosition();

    this.boardView.setIsDragging(true);
  }

  trackMousePosition() {
    document.addEventListener('mousemove', mouseMoveHandler, {
      passive: true,
    });
  }

  untrackMousePosition() {
    document.removeEventListener('mousemove', mouseMoveHandler);
  }

  onDragRelease() {
    this.untrackMousePosition();

    this.boardView.setIsDragging(false);
  }

  trackGroupTask(_: number, task: BoardViewTask) {
    return task?.id;
  }

  // A right click acts on the card under the pointer alone; the toolbar that
  // appears with a selection is what handles several tasks at once.
  onTaskContextMenu(event: MouseEvent, task: BoardViewTask) {
    if (!this.canMove() && !this.canDelete()) return;

    event.preventDefault();

    const menu = this.taskMenu();

    menu.close();
    this.menuTask.set(task);
    menu.open({ x: event.clientX, y: event.clientY });
  }

  onRemoveFromBoardClicked() {
    const task = this.menuTask();
    const board = this.boardView.board();

    this.taskMenu().close();

    if (!task || !board) return;

    this.taskCommands.removeFromBoard(task.id, board.id, {
      boardName: board.name,
    });
  }

  onDeleteTaskClicked() {
    const task = this.menuTask();

    this.taskMenu().close();

    if (!task) return;

    this.taskCommands.deleteMany([task.id]);
  }

  onTaskClicked(
    event: KeyboardEvent | MouseEvent,
    task: Selected<BoardViewTask>,
    groupId: number
  ) {
    const id = task.id;
    const selected = task.selected;

    if (event.shiftKey) {
      if (selected) {
        this.selection.deselectRange(id, groupId);
      } else {
        this.selection.selectRange(id, groupId);
      }
    } else if (event.ctrlKey) {
      if (selected) {
        this.selection.deselect(id);
      } else {
        this.selection.select(id);
      }
    } else {
      this.dialog.open(TaskDetailDialogComponent, {
        width: TaskDetailDialogComponent.width,
        height: TaskDetailDialogComponent.height,
        data: task,
        panelClass: TaskDetailDialogComponent.panelClass,
        autoFocus: false,
      });
    }
  }
}
