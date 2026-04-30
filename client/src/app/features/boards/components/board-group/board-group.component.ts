import { CdkDrag, CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  input,
  OnDestroy,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import * as BoardGroupActions from '@app/core/store/groups/board-groups.actions';
import * as BoardGroupSelectors from '@app/core/store/groups/board-groups.selectors';
import { selectInlineActiveGroupId } from '@app/core/store/groups/board-groups.selectors';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { mouseMoveHandler } from '@boards/util/mouse-move-handler';
import { Selected } from '@core/models/selected';
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
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CdkDropList,
    ScrollShadowVericalDirective,
    BoardGroupCardComponent,
    CdkDrag,
    BoardGroupTaskInlineComponent,
    StrokedButtonComponent,
  ],
  template: `<div
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
            [cdkDragDisabled]="!isAuthenticated()"
            class="board-group-task-card"
            [cdkDragData]="task"
            [task]="task"
            [groupId]="group().id"
            (cdkDragStarted)="onDragStarted()"
            (cdkDragReleased)="onDragRelease()"
            (click)="onTaskClicked($event, task, group().id)">
          </app-board-group-card>
        }

        @if (isInlineActive()) {
          <app-board-group-task-inline
            (canceled)="onInlineCanceled()"
            [boardGroupId]="group().id">
          </app-board-group-task-inline>
        }

        @if (showAddButton()) {
          <div class="h-11.5 p-[.3rem]">
            <button
              app-stroked-button
              color="primary"
              class="block w-full"
              (click)="onAddTaskClicked()">
              ADD TASK
            </button>
          </div>
        } @else {
          <div class="h-11.5 min-h-11.5 w-full">{{ ' ' }}</div>
        }
      </div>
    </div>
  </div> `,
})
export class BoardGroupComponent implements OnDestroy, AfterViewInit {
  private store = inject(Store);
  private dialog = inject(DialogService);

  readonly dragListId = input.required<string>();
  readonly group = input.required<BoardViewGroup>();
  readonly siblingIds = input.required<string[]>();

  readonly container = viewChild.required<ElementRef>('container');

  focused = signal(false);
  isAuthenticated = this.store.selectSignal(selectIsAuthenticated);
  isDragging = this.store.selectSignal(BoardGroupSelectors.selectIsDragging);
  private inlineActiveGroupId = this.store.selectSignal(
    selectInlineActiveGroupId
  );
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
      .pipe(takeUntilDestroyed())
      .subscribe({ next: () => this.focused.set(true) });

    fromEvent(el, 'mouseleave', { passive: true })
      .pipe(takeUntilDestroyed())
      .subscribe({ next: () => this.focused.set(false) });
  }

  ngOnDestroy() {
    document.removeEventListener('mousemove', mouseMoveHandler);
  }

  onAddTaskClicked() {
    this.store.dispatch(
      BoardGroupActions.setInlineActive({ groupId: this.group().id })
    );
  }

  onInlineCanceled() {
    this.store.dispatch(BoardGroupActions.clearInlineActive());
  }

  drop(event: CdkDragDrop<BoardViewTask[]>) {
    const { data: task } = event.item;

    this.store.dispatch(
      BoardGroupActions.moveTaskInBoardGroup({
        request: {
          newGroupId: +event.container.id,
          oldGroupId: +event.previousContainer.id,
          taskId: task.id,
          currentIndex: event.currentIndex,
          previousIndex: event.previousIndex,
        },
      })
    );
  }

  onDragStarted() {
    this.trackMousePosition();

    this.store.dispatch(BoardGroupActions.setIsDragging({ isDragging: true }));
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

    this.store.dispatch(BoardGroupActions.setIsDragging({ isDragging: false }));
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
      this.store.dispatch(
        selected
          ? BoardGroupActions.deSelectTaskBulk({ id, groupId })
          : BoardGroupActions.selectTaskBulk({ id, groupId })
      );
    } else if (event.ctrlKey) {
      this.store.dispatch(
        selected
          ? BoardGroupActions.deSelectTask({ id })
          : BoardGroupActions.selectTask({ id })
      );
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
