import { Component, Input } from '@angular/core';
import { toggleChip } from '@app/core/animations/animations';
import { ProjectTaskDto } from '@app/core/models/view-models/project-task-dto';
import { Store } from '@ngrx/store';
import { AppState } from '@app/core/core.state';
import { ActionEditProjectTask, ActionDeleteProjectTask } from '../../store/project-tasks.actions';

@Component({
  selector: 'app-task-list-item',
  templateUrl: './task-list-item.component.html',
  styleUrls: ['./task-list-item.component.scss'],
  animations: [toggleChip],
})
export class TaskListItemComponent {
  @Input() task: ProjectTaskDto;

  constructor(private store: Store<AppState>) {}

  editClicked() {
    this.store.dispatch(new ActionEditProjectTask(this.task));
  }

  deleteClicked() {
    this.store.dispatch(new ActionDeleteProjectTask(this.task));
  }
}
