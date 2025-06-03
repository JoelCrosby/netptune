import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';

@Component({
    selector: 'app-task-list-group',
    templateUrl: './task-list-group.component.html',
    styleUrls: ['./task-list-group.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class TaskListGroupComponent {
  @Input() groupName!: string;
  @Input() tasks!: TaskViewModel[] | null;
  @Input() header!: string;
  @Input() emptyMessage!: string;

  trackByTask(_: number, task: TaskViewModel) {
    return task.id;
  }
}
