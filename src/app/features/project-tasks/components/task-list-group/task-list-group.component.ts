import { AppState } from '@core/core.state';
import {
  Component,
  OnInit,
  Input,
  ChangeDetectionStrategy,
} from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { Observable } from 'rxjs';
import { fadeIn, dropIn } from '@core/animations/animations';
import {
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { TaskStatus } from '@app/core/enums/project-task-status';
import * as actions from '../../store/tasks.actions';
import { Store } from '@ngrx/store';
import { getNewSortOrder } from '@core/util/sort-order-helper';

@Component({
  selector: 'app-task-list-group',
  templateUrl: './task-list-group.component.html',
  styleUrls: ['./task-list-group.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [fadeIn, dropIn],
})
export class TaskListGroupComponent implements OnInit {
  @Input() groupName: string;
  @Input() tasks: TaskViewModel[] | undefined;
  @Input() header: string;
  @Input() emptyMessage: string;
  @Input() loaded: Observable<boolean>;
  @Input() status: TaskStatus;

  constructor(private store: Store<AppState>) {}

  ngOnInit() {}

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
        task: {
          ...task,
          status,
          sortOrder,
        },
      })
    );
  }
}
