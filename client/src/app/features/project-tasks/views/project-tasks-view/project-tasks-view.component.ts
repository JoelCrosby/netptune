import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { exportTasks } from '@core/store/tasks/tasks.actions';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { HeaderAction } from '@core/types/header-action';
import { TaskDialogComponent } from '@entry/dialogs/task-dialog/task-dialog.component';
import { Store } from '@ngrx/store';
import { first, tap } from 'rxjs/operators';

@Component({
  templateUrl: './project-tasks-view.component.html',
  styleUrls: ['./project-tasks-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectTasksViewComponent implements OnInit, OnDestroy {
  secondaryActions: HeaderAction[] = [
    {
      label: 'Export Tasks',
      click: () => this.onExportTasksClicked(),
      icon: 'get_app',
      iconClass: 'material-icons-round',
    },
  ];

  constructor(
    public dialog: MatDialog,
    private store: Store,
    private hubService: ProjectTasksHubService
  ) {}

  ngOnInit() {
    this.store
      .select(selectCurrentWorkspaceIdentifier)
      .pipe(
        first(),
        tap((identifier) => {
          this.hubService
            .connect()
            .then(() =>
              this.hubService
                .addToGroup(`[workspace] ${identifier}`)
                .subscribe()
            );
        })
      )
      .subscribe();
  }

  ngOnDestroy() {
    this.hubService.disconnect();
  }

  showAddModal() {
    this.dialog.open(TaskDialogComponent, {
      width: '600px',
    });
  }

  onExportTasksClicked() {
    this.store.dispatch(exportTasks());
  }
}
