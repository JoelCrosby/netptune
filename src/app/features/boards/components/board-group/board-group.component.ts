import { CdkDragDrop } from '@angular/cdk/drag-drop';
import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnDestroy,
  ViewChild,
  OnInit,
} from '@angular/core';
import { AppState } from '@app/core/core.state';
import { BoardGroup } from '@app/core/models/board-group';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { Store } from '@ngrx/store';
import { BehaviorSubject, fromEvent, Subject, Observable } from 'rxjs';
import { takeUntil, withLatestFrom, map } from 'rxjs/operators';
import * as BoardGroupActions from '../../store/groups/board-groups.actions';
import * as BoardGroupSelectors from '../../store/groups/board-groups.selectors';

@Component({
  selector: 'app-board-group',
  templateUrl: './board-group.component.html',
  styleUrls: ['./board-group.component.scss'],
})
export class BoardGroupComponent implements OnInit, OnDestroy, AfterViewInit {
  @Input() dragListId: string;
  @Input() group: BoardGroup;
  @Input() siblingIds: string[];

  @ViewChild('container') container: ElementRef;

  focusedSubject = new BehaviorSubject<boolean>(false);
  onDestroy$ = new Subject();

  focused$: Observable<boolean>;
  isDragging$: Observable<boolean>;
  isInlineActive$: Observable<boolean>;

  showAddButton$: Observable<boolean>;

  constructor(private store: Store<AppState>) {}

  ngOnInit() {
    this.focused$ = this.focusedSubject.pipe();
    this.isDragging$ = this.store.select(BoardGroupSelectors.selectIsDragging);
    this.isInlineActive$ = this.store.select(
      BoardGroupSelectors.selectIsInlineActive
    );

    this.showAddButton$ = this.focused$.pipe(
      withLatestFrom(this.isDragging$, this.isInlineActive$),
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
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  onAddTaskClicked() {
    this.store.dispatch(
      BoardGroupActions.setIsInlineActive({ isInlineActive: true })
    );
  }

  onInlineCanceled() {
    this.store.dispatch(
      BoardGroupActions.setIsInlineActive({ isInlineActive: false })
    );
  }

  drop(event: CdkDragDrop<TaskViewModel[]>) {
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
    this.store.dispatch(BoardGroupActions.setIsDragging({ isDragging: true }));
  }

  onDragEnded() {
    this.store.dispatch(BoardGroupActions.setIsDragging({ isDragging: false }));
  }
}
