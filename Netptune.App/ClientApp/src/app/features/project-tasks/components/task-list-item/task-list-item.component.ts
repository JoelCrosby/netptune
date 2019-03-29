import { Component, Input } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { toggleChip } from '@app/core/animations/animations';
import { ProjectTaskDto } from '@app/core/models/view-models/project-task-dto';
import { ProjectTaskStatus } from '@app/core/enums/project-task-status';

@Component({
  selector: 'app-task-list-item',
  templateUrl: './task-list-item.component.html',
  styleUrls: ['./task-list-item.component.scss'],
  animations: [toggleChip],
})
export class TaskListItemComponent {
  @Input() task: ProjectTaskDto;

  constructor(public dialog: MatDialog, public snackBar: MatSnackBar) {}

  getStatusClass(task: ProjectTaskDto): string {
    switch (task.status) {
      case ProjectTaskStatus.Complete:
        return 'fas fa-check completed';
      case ProjectTaskStatus.InProgress:
        return 'fas fa-minus in-progress';
      case ProjectTaskStatus.OnHold:
        return 'fas fa-minus-circle blocked';
      default:
        return 'fas fa-stream none';
    }
  }
}
