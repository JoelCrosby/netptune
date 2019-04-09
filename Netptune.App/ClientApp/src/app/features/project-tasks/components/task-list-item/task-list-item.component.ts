import { Component, Input } from '@angular/core';
import { toggleChip } from '@app/core/animations/animations';
import { ProjectTaskDto } from '@app/core/models/view-models/project-task-dto';

@Component({
  selector: 'app-task-list-item',
  templateUrl: './task-list-item.component.html',
  styleUrls: ['./task-list-item.component.scss'],
  animations: [toggleChip],
})
export class TaskListItemComponent {
  @Input() task: ProjectTaskDto;
}
