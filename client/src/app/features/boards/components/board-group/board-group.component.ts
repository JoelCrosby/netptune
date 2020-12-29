import { CdkDragDrop } from '@angular/cdk/drag-drop';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Input,
  NgZone,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import * as BoardGroupActions from '@boards/store/groups/board-groups.actions';
import * as BoardGroupSelectors from '@boards/store/groups/board-groups.selectors';
import { mouseMoveHandler } from '@boards/util/mouse-move-handler';
import {
  BoardViewGroup,
  BoardViewTask,
} from '@core/models/view-models/board-view';
import { Store } from '@ngrx/store';
import {
  BehaviorSubject,
  combineLatest,
  fromEvent,
  Observable,
  Subject,
} from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-board-group',
  templateUrl: './board-group.component.html',
  styleUrls: ['./board-group.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupComponent implements OnInit, OnDestroy, AfterViewInit {
  @Input() dragListId: string;
  @Input() group: BoardViewGroup;
  @Input() siblingIds: string[];

  @ViewChild('container') container: ElementRef;

  focusedSubject = new BehaviorSubject<boolean>(false);
  onDestroy$ = new Subject();

  focused$: Observable<boolean>;
  isDragging$: Observable<boolean>;
  isInlineActive$: Observable<boolean>;

  showAddButton$: Observable<boolean>;

  constructor(private store: Store, private zone: NgZone) {}

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
    const el = this.container.nativeElement;

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
}
