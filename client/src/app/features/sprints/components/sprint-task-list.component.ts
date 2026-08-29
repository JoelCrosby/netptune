import { Component, computed, inject, input } from '@angular/core';
import { SprintStatus } from '@core/enums/sprint-status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { SprintCommandsService } from '@core/services/sprint-commands.service';
import { taskColumns, taskNameCell } from '@core/tasks/task-columns';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableColumn } from '@static/components/datatable/datatable.types';
import { TaskTableComponent } from '@static/components/task-table.component';

@Component({
  selector: 'app-sprint-task-list',
  imports: [
    StrokedButtonComponent,
    DatatableCellTemplateDirective,
    TaskTableComponent,
  ],
  template: `
    <app-task-table
      containerClass="overflow-auto rounded-lg shadow-sm"
      key="sprint-tasks"
      url="api/tasks"
      tableClass="min-w-[820px] table-fixed"
      i18n-emptyMessage="Empty state for the sprint task list"
      emptyMessage="No tasks in this sprint."
      [columns]="columns()"
      [params]="params"
      [stickyHeader]="true">
      <ng-template appDatatableCell="actions" let-task>
        @if (!task.isArchived) {
          <button
            app-stroked-button
            color="primary"
            type="button"
            class="h-6 text-xs"
            [disabled]="updateLoading()"
            (click)="onRemoveTask(task.id)">
            <span i18n="Button that removes the task from the sprint">
              Remove
            </span>
          </button>
        }
      </ng-template>
    </app-task-table>
  `,
})
export class SprintTaskListComponent {
  readonly sprint = input.required<SprintDetailViewModel>();
  readonly canManage = input.required<boolean>();

  readonly sprintStatus = SprintStatus;
  private readonly sprintCommands = inject(SprintCommandsService);

  readonly updateLoading = this.sprintCommands.isUpdating;
  readonly canEditSprintTasks = computed(() => {
    return this.canManage() && this.sprint().status !== SprintStatus.completed;
  });

  // Archived tasks stay in the list: the sprint is a record of the work it held, and dropping them
  // on archive would silently rewrite its history. They are badged and cannot be acted on.
  readonly params = computed(() => ({
    sprintId: this.sprint().id,
    includeArchived: true,
  }));

  private readonly baseColumns = taskColumns<TaskViewModel>(
    ['systemId', 'name', 'project', 'status', 'priority'],
    {
      overrides: {
        name: taskNameCell<TaskViewModel>({
          link: (task) =>
            task.isArchived ? null : ['../../tasks', task.systemId],
          archived: (task) => task.isArchived ?? false,
        }),
        project: { widthClass: 'w-48' },
      },
    }
  );

  private readonly actionsColumn: DatatableColumn<TaskViewModel> = {
    id: 'actions',
    header: '',
    widthClass: 'w-28',
    align: 'end',
  };

  readonly columns = computed<DatatableColumn<TaskViewModel>[]>(() => {
    return this.canEditSprintTasks()
      ? [...this.baseColumns, this.actionsColumn]
      : this.baseColumns;
  });

  onRemoveTask(taskId?: number) {
    const sprintId = this.sprint().id;
    if (!sprintId || !taskId) return;
    this.sprintCommands.removeTask(sprintId, taskId);
  }
}
