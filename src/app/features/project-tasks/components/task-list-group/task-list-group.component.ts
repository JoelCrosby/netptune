import {
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
} from '@angular/core';
import { TaskStatus } from '@core/enums/project-task-status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import * as actions from '@core/store/tasks/tasks.actions';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { getNewSortOrder } from '@core/util/sort-order-helper';
import { Store } from '@ngrx/store';
import { first, tap } from 'rxjs/operators';

@Component({
  selector: 'app-task-list-group',
  templateUrl: './task-list-group.component.html',
  styleUrls: ['./task-list-group.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListGroupComponent implements OnInit {
  @Input() groupName: string;
  @Input() tasks: TaskViewModel[];
  @Input() header: string;
  @Input() emptyMessage: string;
  @Input() status: TaskStatus;

  workspaceIdentifier: string;

  constructor(private store: Store) {}

  ngOnInit() {
    this.store
      .select(selectCurrentWorkspaceIdentifier)
      .pipe(
        first(),
        tap((identifier) => {
          this.workspaceIdentifier = identifier;
        })
      )
      .subscribe();
  }

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
        identifier: `[workspace] ${this.workspaceIdentifier}`,
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
      actions.deleteProjectTask({
        identifier: `[workspace] ${this.workspaceIdentifier}`,
        task,
      })
    );
  }

  markCompleteClicked(task: TaskViewModel) {
    this.store.dispatch(
      actions.editProjectTask({
        identifier: `[workspace] ${this.workspaceIdentifier}`,
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
        identifier: `[workspace] ${this.workspaceIdentifier}`,
        task: {
          ...task,
          status: TaskStatus.InActive,
        },
      })
    );
  }
}
