import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';

@Component({
  selector: 'app-task-list-item',
  templateUrl: './task-list-item.component.html',
  styleUrls: ['./task-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListItemComponent {
  @Input() task: TaskViewModel;

  constructor(public dialog: MatDialog) {}

  titleClicked() {
    this.dialog.open(TaskDetailDialogComponent, {
      width: '800px',
      data: this.task,
    });
  }
}
