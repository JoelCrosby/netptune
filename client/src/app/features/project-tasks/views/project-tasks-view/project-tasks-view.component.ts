import { Component, OnDestroy, inject, signal } from '@angular/core';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { netptunePermissions } from '@core/auth/permissions';
import { DialogService } from '@core/services/dialog.service';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { HeaderAction } from '@core/types/header-action';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';
import { LucideFolderDown } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { TaskListComponent } from '@project-tasks/components/task-list/task-list.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  selector: 'app-project-tasks-view',
  imports: [PageContainerComponent, PageHeaderComponent, TaskListComponent],
  template: `
    <app-page-container>
      @if (canCreateTasks()) {
        <app-page-header
          i18n-title="Page title for the task list"
          title="Tasks"
          i18n-actionTitle="Button that opens the create-task dialog"
          actionTitle="Create Task"
          (actionClick)="showAddModal()"
          [count]="count()"
          [overflowActions]="secondaryActions" />
      } @else {
        <app-page-header
          i18n-title="Page title for the task list"
          title="Tasks"
          [count]="count()"
          [overflowActions]="secondaryActions" />
      }

      <app-task-list (countChange)="count.set($event)" />
    </app-page-container>
  `,
})
export class ProjectTasksViewComponent implements OnDestroy {
  dialog = inject(DialogService);
  private store = inject(Store);
  private taskCommands = inject(TaskCommandsService);
  private hubService = inject(ProjectTasksHubService);

  readonly count = signal<number | null>(null);

  workspaceId = this.store.selectSignal(selectCurrentWorkspaceIdentifier);
  canCreateTasks = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.create)
  );

  secondaryActions: HeaderAction[] = [
    {
      label: $localize`:Overflow action that downloads the task list as CSV:Export Tasks`,
      click: () => this.onExportTasksClicked(),
      icon: LucideFolderDown,
    },
  ];

  constructor() {
    const identifier = this.workspaceId();

    if (identifier) {
      this.hubService.addToGroup(identifier);
    }
  }

  ngOnDestroy() {
    this.hubService.leaveGroup();
  }

  showAddModal() {
    this.dialog.open(CreateTaskDialogComponent, {
      width: CreateTaskDialogComponent.width,
    });
  }

  onExportTasksClicked() {
    this.taskCommands.export();
  }
}
