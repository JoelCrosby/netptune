import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { PERMISSIONS } from '@core/auth/permissions';
import { DialogService } from '@core/services/dialog.service';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { ProjectTasksHubService } from '@core/services/tasks-hub.service';
import { HeaderAction } from '@core/types/header-action';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';
import { LucideFolderDown } from '@lucide/angular';
import { TaskListComponent } from '@project-tasks/components/task-list/task-list.component';
import { TaskListFiltersComponent } from '@project-tasks/components/task-list/task-list-filters.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  selector: 'app-project-tasks-view',
  imports: [
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    TaskListComponent,
    TaskListFiltersComponent,
  ],
  template: `
    <app-page-container layout="list">
      @if (canCreateTasks()) {
        <app-page-header
          toolbar
          i18n-title="Page title for the task list"
          title="Tasks"
          i18n-actionTitle="Button that opens the create-task dialog"
          actionTitle="Create Task"
          i18n-filtersLabel="Accessible name of the task list filter row"
          filtersLabel="Filter tasks"
          (actionClick)="showAddModal()"
          [count]="count()"
          [overflowActions]="secondaryActions()">
          <app-task-list-filters pageHeaderFilters />
        </app-page-header>
      } @else {
        <app-page-header
          toolbar
          i18n-title="Page title for the task list"
          title="Tasks"
          i18n-filtersLabel="Accessible name of the task list filter row"
          filtersLabel="Filter tasks"
          [count]="count()"
          [overflowActions]="secondaryActions()">
          <app-task-list-filters pageHeaderFilters />
        </app-page-header>
      }

      <app-page-body>
        <app-task-list (countChange)="count.set($event)" />
      </app-page-body>
    </app-page-container>
  `,
})
export class ProjectTasksViewComponent implements OnDestroy {
  dialog = inject(DialogService);
  private taskCommands = inject(TaskCommandsService);
  private hubService = inject(ProjectTasksHubService);

  readonly count = signal<number | null>(null);

  workspaceId = inject(CurrentWorkspaceService).slug;
  canCreateTasks = hasPermission(PERMISSIONS.tasks.create);

  private canExportTasks = hasPermission(PERMISSIONS.tasks.export);

  secondaryActions = computed<HeaderAction[]>(() => {
    if (!this.canExportTasks()) return [];

    return [
      {
        label: $localize`:Overflow action that downloads the task list as CSV:Export Tasks`,
        click: () => this.onExportTasksClicked(),
        icon: LucideFolderDown,
      },
    ];
  });

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
