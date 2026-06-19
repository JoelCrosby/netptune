import { Component, computed, inject } from '@angular/core';
import { TaskStatus } from '@core/enums/project-task-status';
import { SprintStatus } from '@core/enums/sprint-status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { initBacklogView } from '@core/store/sprints/sprints.actions';
import {
  selectAllSprints,
  selectBacklogTasks,
  selectBacklogTasksLoading,
} from '@core/store/sprints/sprints.selectors';
import { dispatchForWorkspace } from '@core/util/dispatch-for-workspace';
import { Store } from '@ngrx/store';
import { CardComponent } from '@static/components/card/card.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import {
  BacklogGroup,
  SprintBacklogGroupComponent,
} from '../../components/sprint-backlog-group.component';

@Component({
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    SpinnerComponent,
    CardComponent,
    SprintBacklogGroupComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header title="Backlog" />

      @if (loading()) {
        <div class="flex h-full flex-col items-center justify-center">
          <app-spinner diameter="32px" />
        </div>
      } @else {
        <div class="flex flex-col gap-6">
          @if (assignableSprints().length === 0) {
            <div
              class="text-muted border-border rounded border-2 border-dashed p-4 text-sm">
              No planning or active sprints found. Create a sprint first to
              assign tasks to it.
            </div>
          }

          @for (group of groups(); track group.status) {
            <app-sprint-backlog-group
              [group]="group"
              [sprints]="assignableSprints()" />
          } @empty {
            <app-card class="text-muted min-h-0 text-center">
              The backlog is empty — all tasks are assigned to sprints.
            </app-card>
          }
        </div>
      }
    </app-page-container>
  `,
})
export class SprintBacklogViewComponent {
  private store = inject(Store);

  readonly loading = this.store.selectSignal(selectBacklogTasksLoading);
  readonly backlogTasks = this.store.selectSignal(selectBacklogTasks);
  readonly allSprints = this.store.selectSignal(selectAllSprints);

  readonly assignableSprints = computed(() =>
    this.allSprints().filter(
      (s) =>
        s.status === SprintStatus.planning || s.status === SprintStatus.active
    )
  );

  readonly groups = computed((): BacklogGroup[] => {
    const tasks = this.backlogTasks();
    const statusOrder = [
      TaskStatus.new,
      TaskStatus.inProgress,
      TaskStatus.onHold,
    ];
    const labelMap: Record<number, string> = {
      [TaskStatus.new]: 'New',
      [TaskStatus.inProgress]: 'In Progress',
      [TaskStatus.onHold]: 'Other',
    };

    const grouped = new Map<TaskStatus, TaskViewModel[]>([
      [TaskStatus.new, []],
      [TaskStatus.inProgress, []],
      [TaskStatus.onHold, []],
    ]);

    for (const task of tasks) {
      const key = statusOrder.includes(task.status)
        ? task.status
        : TaskStatus.onHold;
      grouped.get(key)?.push(task);
    }

    return statusOrder
      .filter((s) => (grouped.get(s)?.length ?? 0) > 0)
      .map((s) => ({
        label: labelMap[s],
        status: s,
        tasks: grouped.get(s) ?? [],
      }));
  });

  constructor() {
    dispatchForWorkspace(() => initBacklogView());
  }
}
