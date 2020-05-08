import { CdkDragDrop } from '@angular/cdk/drag-drop';
import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnDestroy,
  ViewChild,
} from '@angular/core';
import { AppState } from '@app/core/core.state';
import { BoardGroup } from '@app/core/models/board-group';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { Store } from '@ngrx/store';
import { BehaviorSubject, fromEvent, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { moveTaskInBoardGroup } from '../../store/groups/board-groups.actions';

@Component({
  selector: 'app-board-group',
  templateUrl: './board-group.component.html',
  styleUrls: ['./board-group.component.scss'],
})
export class BoardGroupComponent implements OnDestroy, AfterViewInit {
  @Input() dragListId: string;
  @Input() group: BoardGroup;
  @Input() siblingIds: string[];

  @ViewChild('container') container: ElementRef;

  focusedSubject = new BehaviorSubject<boolean>(false);
  onDestroy$ = new Subject();

  focused$ = this.focusedSubject.pipe();

  inlineActive = false;

  constructor(private store: Store<AppState>) {}

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

  onInlineCanceled() {
    this.inlineActive = false;
  }

  drop(event: CdkDragDrop<TaskViewModel[]>) {
    const { data: task } = event.item;

    this.store.dispatch(
      moveTaskInBoardGroup({
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
}
