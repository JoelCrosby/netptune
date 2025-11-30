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
import { MatButton } from '@angular/material/button';
import * as BoardGroupActions from '@boards/store/groups/board-groups.actions';
import * as BoardGroupSelectors from '@boards/store/groups/board-groups.selectors';
import { selectIsInlineActive } from '@boards/store/groups/board-groups.selectors';
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

@Component({
  selector: 'app-board-group',
  templateUrl: './board-group.component.html',
  styleUrls: ['./board-group.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CdkDropList,
    ScrollShadowVericalDirective,
    BoardGroupCardComponent,
    CdkDrag,
    BoardGroupTaskInlineComponent,
    MatButton,
  ],
})
export class BoardGroupComponent implements OnDestroy, AfterViewInit {
  private store = inject(Store);
  private dialog = inject(DialogService);

  readonly dragListId = input.required<string>();
  readonly group = input.required<BoardViewGroup>();
  readonly siblingIds = input.required<string[]>();

  readonly container = viewChild.required<ElementRef>('container');

  focused = signal(false);
  isDragging = this.store.selectSignal(BoardGroupSelectors.selectIsDragging);
  isInlineActive = computed(() => {
    const groupId = this.group().id;
    const inline = this.store.selectSignal(selectIsInlineActive({ groupId }));

    return inline();
  });

  showAddButton = computed(() => {
    return this.focused() && !this.isDragging() && !this.isInlineActive();
  });

  ngAfterViewInit() {
    const el: HTMLDivElement = this.container().nativeElement;

    fromEvent(el, 'mouseenter', { passive: true }).subscribe({
      next: () => this.focused.set(true),
    });

    fromEvent(el, 'mouseleave', { passive: true }).subscribe({
      next: () => this.focused.set(false),
    });
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
