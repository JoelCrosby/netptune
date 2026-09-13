import { Component, computed, inject, viewChild } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { taskColumns, taskNameCell } from '@core/tasks/task-columns';
import { scrollHeights } from '@static/components/datatable/datatable-classes';
import { TaskTableComponent } from '@static/components/task-table.component';

@Component({
  selector: 'app-dashboard-assigned-tasks',
  imports: [TaskTableComponent],
  template: `
    <section class="flex flex-col gap-3">
      <h2 class="text-foreground flex items-center gap-2 text-lg font-semibold">
        <span i18n="Heading of the dashboard card listing your tasks">
          Assigned to me
        </span>
        <span class="text-muted text-sm font-normal">{{ totalCount() }}</span>
      </h2>

      <app-task-table
        key="dashboard-assigned-tasks"
        url="api/tasks"
        tableClass="md:min-w-[820px] table-fixed"
        i18n-emptyMessage="Empty state for the assigned-tasks card"
        emptyMessage="You have no tasks assigned to you."
        [containerClass]="scrollHeights.panel"
        [columns]="columns"
        [params]="params"
        [stickyHeader]="true" />
    </section>
  `,
})
export class DashboardAssignedTasksComponent {
  readonly scrollHeights = scrollHeights;

  private readonly table = viewChild(TaskTableComponent<TaskViewModel>);
  readonly totalCount = computed(() => this.table()?.loadedCount() ?? null);

  readonly currentUserId = inject(SessionService).currentUserId;

  readonly params = computed(() => {
    const userId = this.currentUserId();

    return userId ? { assignees: [userId] } : {};
  });

  readonly columns = taskColumns<TaskViewModel>(
    ['systemId', 'name', 'project', 'sprint', 'status', 'priority'],
    {
      overrides: {
        name: taskNameCell<TaskViewModel>({
          link: (task) => ['../tasks', task.systemId],
        }),
        project: { widthClass: 'w-48' },
        sprint: { widthClass: 'w-38' },
      },
    }
  );
}
