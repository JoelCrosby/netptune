import { moveTaskInBoardGroup } from './../../store/groups/board-groups.actions';
import { TaskViewModel } from './../../../../core/models/view-models/project-task-dto';
import { takeUntil } from 'rxjs/operators';
import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { BoardGroup } from '@app/core/models/board-group';
import { BehaviorSubject, fromEvent, Subject } from 'rxjs';
import {
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { getNewSortOrder } from '@app/core/util/sort-order-helper';
import { AppState } from '@app/core/core.state';
import { Store } from '@ngrx/store';

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

  focused$ = this.focusedSubject.pipe();

  inlineActive = false;

  constructor(private store: Store<AppState>) {}

  ngOnInit() {
    console.log({
      id: this.group.id,
      name: this.group.name,
      siblingIds: this.siblingIds,
    });
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

  onInlineCanceled() {
    this.inlineActive = false;
  }

  drop(event: CdkDragDrop<TaskViewModel[]>) {
    if (event.previousContainer === event.container) {
      moveItemInArray(
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    } else {
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    }

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

    this.moveTask(data, order);
  }

  // TODO: Implement logic for creating request model based on drag containers
  moveTask(boardGroup: BoardGroup, sortOrder: number) {
    // this.store.dispatch(moveTaskInBoardGroup({
    //   request: {
    //     newGroupId:
    //   }
    // }))
  }
}
