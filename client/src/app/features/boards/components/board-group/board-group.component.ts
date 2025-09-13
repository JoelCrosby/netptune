import { CdkDragDrop, CdkDropList, CdkDrag } from '@angular/cdk/drag-drop';
import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, Input, NgZone, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import * as BoardGroupActions from '@boards/store/groups/board-groups.actions';
import * as BoardGroupSelectors from '@boards/store/groups/board-groups.selectors';
import { mouseMoveHandler } from '@boards/util/mouse-move-handler';
import { Selected } from '@core/models/selected';
import {
  BoardViewGroup,
  BoardViewTask,
} from '@core/models/view-models/board-view';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { Store } from '@ngrx/store';
import {
  BehaviorSubject,
  combineLatest,
  fromEvent,
  Observable,
  Subject,
} from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { ScrollShadowVericalDirective } from '@static/directives/scroll-shadow-vertical.directive';
import { AsyncPipe } from '@angular/common';
import { BoardGroupCardComponent } from '../board-group-card/board-group-card.component';
import { BoardGroupTaskInlineComponent } from '../board-group-task-inline/board-group-task-inline.component';
import { MatButton } from '@angular/material/button';

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
    AsyncPipe
],
})
export class BoardGroupComponent implements OnInit, OnDestroy, AfterViewInit {
  private store = inject(Store);
  private dialog = inject(DialogService);
  private zone = inject(NgZone);

  @Input() dragListId!: string;
  @Input() group!: BoardViewGroup;
  @Input() siblingIds!: string[];

  @ViewChild('container') container!: ElementRef;

  focusedSubject = new BehaviorSubject<boolean>(false);
  onDestroy$ = new Subject<void>();

  focused$!: Observable<boolean>;
  isDragging$!: Observable<boolean>;
  isInlineActive$!: Observable<boolean>;

  showAddButton$!: Observable<boolean>;

  dragging = false;

  ngOnInit() {
    this.focused$ = this.focusedSubject.pipe();
    this.isDragging$ = this.store.select(BoardGroupSelectors.selectIsDragging);

    this.isInlineActive$ = this.store.select(
      BoardGroupSelectors.selectIsInlineActive,
      { groupId: this.group.id }
    );

    this.showAddButton$ = combineLatest([
      this.focused$,
      this.isDragging$,
      this.isInlineActive$,
    ]).pipe(
      map(
        ([focused, isDragging, isInlineActive]) =>
          focused && !isDragging && !isInlineActive
      )
    );
  }

  ngAfterViewInit() {
    const el: HTMLDivElement = this.container.nativeElement;

    fromEvent(el, 'mouseenter', { passive: true })
      .pipe(takeUntil(this.onDestroy$))
      .subscribe({
        next: () => this.focusedSubject.next(true),
      });

    fromEvent(el, 'mouseleave', { passive: true })
      .pipe(takeUntil(this.onDestroy$))
      .subscribe({
        next: () => this.focusedSubject.next(false),
      });
  }

  ngOnDestroy() {
    document.removeEventListener('mousemove', mouseMoveHandler);

    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  onAddTaskClicked() {
    this.store.dispatch(
      BoardGroupActions.setInlineActive({ groupId: this.group.id })
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
    this.dragging = true;
    this.trackMousePosition();

    this.zone.run(() => {
      this.store.dispatch(
        BoardGroupActions.setIsDragging({ isDragging: true })
      );
    });
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

    this.zone.run(() => {
      this.store.dispatch(
        BoardGroupActions.setIsDragging({ isDragging: false })
      );
    });
  }

  trackGroupTask(_: number, task: BoardViewTask) {
    return task?.id;
  }

  onTaskClicked(
    event: KeyboardEvent | MouseEvent,
    task: Selected<BoardViewTask>,
    groupId: number
  ) {
    if (this.dragging) {
      this.dragging = false;
      return;
    }

    this.dragging = false;

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
