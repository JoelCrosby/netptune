import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { TaskDialogComponent } from '@entry/dialogs/task-dialog/task-dialog.component';

@Component({
  templateUrl: './project-tasks-view.component.html',
  styleUrls: ['./project-tasks-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectTasksViewComponent {
  constructor(public dialog: MatDialog) {}

  showAddModal() {
    this.dialog
      .open(TaskDialogComponent, {
        width: '600px',
      })
      .afterClosed()
      .subscribe(() => {});
  }
}
