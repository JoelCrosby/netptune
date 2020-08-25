import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { TaskDialogComponent } from '@entry/dialogs/task-dialog/task-dialog.component';
import { HeaderAction } from '@app/core/types/header-action';
import { Store } from '@ngrx/store';
import { exportTasks } from '@app/core/store/tasks/tasks.actions';

@Component({
  templateUrl: './project-tasks-view.component.html',
  styleUrls: ['./project-tasks-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectTasksViewComponent {
  secondaryActions: HeaderAction[] = [
    {
      label: 'Export Tasks',
      click: () => this.onExportTasksClicked(),
      icon: 'get_app',
      iconClass: 'material-icons-round',
    },
  ];

  constructor(public dialog: MatDialog, private store: Store) {}

  showAddModal() {
    this.dialog
      .open(TaskDialogComponent, {
        width: '600px',
      })
      .afterClosed()
      .subscribe(() => {});
  }

  onExportTasksClicked() {
    this.store.dispatch(exportTasks());
  }
}
