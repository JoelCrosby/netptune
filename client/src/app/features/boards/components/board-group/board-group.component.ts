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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BoardGroupCommandsService } from '@core/services/board-group-commands.service';
import { BoardComposerService } from '@core/services/board-composer.service';
import { BoardSelectionService } from '@core/services/board-selection.service';
import { BoardViewService } from '@core/services/board-view.service';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { mouseMoveHandler } from '@boards/util/mouse-move-handler';
import { Selected } from '@core/models/selected';
import { Status } from '@core/models/status';
import {
  BoardViewGroup,
  BoardViewTask,
} from '@core/models/view-models/board-view';
import { DialogService } from '@core/services/dialog.service';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { Store } from '@ngrx/store';
import { ScrollShadowVericalDirective } from '@static/directives/scroll-shadow-vertical.directive';
import { fromEvent } from 'rxjs';
import { BoardGroupCardComponent } from '../board-group-card/board-group-card.component';
import { BoardGroupTaskInlineComponent } from '../board-group-task-inline/board-group-task-inline.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';

@Component({
  selector: 'app-board-group',
  styles: `
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
  ],
  template: `
    <div
      class="border-border bg-board-group relative flex h-full flex-1 flex-col rounded border">
      <ng-content />

      <div #container class="h-full flex-1">
        <div
          cdkDropList
          appScrollShadowVertical
          class="custom-scroll board-task-list flex h-[calc(100vh-267px)] flex-col overflow-y-auto p-[.6rem]"
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
  `,
})
export class BoardGroupComponent implements OnDestroy, AfterViewInit {
  private store = inject(Store);
  private boardView = inject(BoardViewService);
  private selection = inject(BoardSelectionService);
  private composer = inject(BoardComposerService);
  private boardCommands = inject(BoardGroupCommandsService);
  private dialog = inject(DialogService);
  private destroyRef = inject(DestroyRef);

  readonly dragListId = input.required<string>();
  readonly group = input.required<BoardViewGroup>();
  readonly assignedStatus = input<Status | null>(null);
  readonly siblingIds = input.required<string[]>();
  readonly reorderDisabled = input(false);

  readonly container = viewChild.required<ElementRef>('container');

  readonly dragDisabled = computed(() => {
    return !this.isAuthenticated() || this.reorderDisabled();
  });

  focused = signal(false);
  isAuthenticated = this.store.selectSignal(selectIsAuthenticated);
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
        data: task,
        panelClass: 'app-modal-class',
        autoFocus: false,
      });
    }
  }
}
