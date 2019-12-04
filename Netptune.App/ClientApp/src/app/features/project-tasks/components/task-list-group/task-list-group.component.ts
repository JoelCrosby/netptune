import { AppState } from './../../../../core/core.state';
import { Component, OnInit, Input } from '@angular/core';
import { ProjectTaskDto } from '@core/models/view-models/project-task-dto';
import { Observable } from 'rxjs';
import { fadeIn, dropIn } from '@core/animations/animations';
import {
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { ProjectTaskStatus } from '@app/core/enums/project-task-status';
import { ActionEditProjectTask } from '../../store/project-tasks.actions';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-task-list-group',
  templateUrl: './task-list-group.component.html',
  styleUrls: ['./task-list-group.component.scss'],
  animations: [fadeIn, dropIn],
})
export class TaskListGroupComponent implements OnInit {
  @Input() groupName: string;
  @Input() tasks: ProjectTaskDto[] | undefined;
  @Input() header: string;
  @Input() emptyMessage: string;
  @Input() loaded: Observable<boolean>;
  @Input() status: ProjectTaskStatus;

  constructor(private store: Store<AppState>) {}

  ngOnInit() {}

  drop(
    event: CdkDragDrop<{ tasks: ProjectTaskDto[]; status: ProjectTaskStatus }>
  ) {
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

      const { status } = event.container.data;
      const { data } = event.item;

      this.moveTask(status, data);
    }
  }

  moveTask(status: ProjectTaskStatus, task: ProjectTaskDto) {
    this.store.dispatch(
      new ActionEditProjectTask({
        ...task,
        status,
      })
    );
  }
}
