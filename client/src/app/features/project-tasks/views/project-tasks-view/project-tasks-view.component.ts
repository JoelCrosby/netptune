import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  inject,
} from '@angular/core';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { DialogService } from '@core/services/dialog.service';
import { exportTasks, loadProjectTasks } from '@core/store/tasks/tasks.actions';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { selectTasksLoading } from '@core/store/tasks/tasks.selectors';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { HeaderAction } from '@core/types/header-action';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';
import { Store } from '@ngrx/store';
import { TaskListComponent } from '@project-tasks/components/task-list/task-list.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  templateUrl: './project-tasks-view.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    MatProgressSpinner,
    TaskListComponent,
  ],
})
export class ProjectTasksViewComponent implements OnDestroy {
  dialog = inject(DialogService);
  private store = inject(Store);
  private hubService = inject(ProjectTasksHubService);

  loading = this.store.selectSignal(selectTasksLoading);
  workspaceId = this.store.selectSignal(selectCurrentWorkspaceIdentifier);

  secondaryActions: HeaderAction[] = [
    {
      label: 'Export Tasks',
      click: () => this.onExportTasksClicked(),
      icon: 'get_app',
      iconClass: 'material-icons-round',
    },
  ];

  constructor() {
    this.hubService.connect().then(() => {
      const identifier = this.workspaceId();

      if (!identifier) return;

      return this.hubService.addToGroup(identifier);
    });

    this.store.dispatch(loadProjectTasks());
  }

  ngOnDestroy() {
    void this.hubService.disconnect();
  }

  showAddModal() {
    this.dialog.open(CreateTaskDialogComponent, {
      width: '600px',
    });
  }

  onExportTasksClicked() {
    this.store.dispatch(exportTasks());
  }
}
