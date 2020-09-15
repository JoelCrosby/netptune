import {
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TaskStatus } from '@core/enums/project-task-status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import * as actions from '@core/store/tasks/tasks.actions';
import { getNewSortOrder } from '@core/util/sort-order-helper';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-task-list-group',
  templateUrl: './task-list-group.component.html',
  styleUrls: ['./task-list-group.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListGroupComponent {
  @Input() groupName: string;
  @Input() tasks: TaskViewModel[];
  @Input() header: string;
  @Input() emptyMessage: string;
  @Input() status: TaskStatus;

  constructor(private store: Store) {}

  trackByTask(_: number, task: TaskViewModel) {
    return task.id;
  }

  drop(event: CdkDragDrop<{ tasks: TaskViewModel[]; status: TaskStatus }>) {
    if (event.previousContainer === event.container) {
      moveItemInArray(
        event.container.data.tasks,
        event.previousIndex,
        event.currentIndex
      );
    } else {
      transferArrayItem(
        event.previousContainer.data.tasks,
        event.container.data.tasks,
        event.previousIndex,
        event.currentIndex
      );
    }

    const tasks = event.container.data.tasks;

    const prevTask = tasks[event.currentIndex - 1];
    const nextTask = tasks[event.currentIndex + 1];

    const preOrder = prevTask && prevTask.sortOrder;
    const nextOrder = nextTask && nextTask.sortOrder;

    const order = getNewSortOrder(preOrder, nextOrder);

    const { status } = event.container.data;
    const { data } = event.item;

    if (data.sortOrder === order && data.status === status) {
      return;
    }

    this.moveTask(data, status, order);
  }

  moveTask(task: TaskViewModel, status: TaskStatus, sortOrder: number) {
    this.store.dispatch(
      actions.editProjectTask({
        identifier: '[none]',
        task: {
          ...task,
          status,
          sortOrder,
        },
      })
    );
  }

  deleteClicked(task: TaskViewModel) {
    this.store.dispatch(
      actions.deleteProjectTask({ identifier: '[none]', task })
    );
  }

  markCompleteClicked(task: TaskViewModel) {
    this.store.dispatch(
      actions.editProjectTask({
        identifier: '[none]',
        task: {
          ...task,
          status: TaskStatus.Complete,
        },
      })
    );
  }

  moveToBacklogClicked(task: TaskViewModel) {
    this.store.dispatch(
      actions.editProjectTask({
        identifier: '[none]',
        task: {
          ...task,
          status: TaskStatus.InActive,
        },
      })
    );
  }
}
