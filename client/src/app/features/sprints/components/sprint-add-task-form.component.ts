import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
} from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { addTasksToSprint } from '@core/store/sprints/sprints.actions';
import {
  selectAvailableSprintTasks,
  selectSprintUpdateLoading,
} from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { FormSelectSearchComponent } from '@static/components/form-select-search/form-select-search.component';

@Component({
  selector: 'app-sprint-add-task-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FlatButtonComponent, FormSelectSearchComponent],
  template: `
    <form
      class="border-border flex flex-wrap items-center gap-3 border-b py-4"
      (submit)="onSubmit($event)">
      <app-form-select-search
        class="min-w-64 flex-1"
        label="Add Task"
        placeholder="Select a task"
        emptyMessage="No matching tasks"
        [options]="availableTasks()"
        [labelWith]="taskLabel"
        [valueWith]="taskValue"
        [value]="selectedTaskId ?? null"
        (changed)="selectedTaskId = $event">
      </app-form-select-search>

      <button
        app-flat-button
        color="primary"
        type="submit"
        class="mt-2"
        [disabled]="updateLoading() || !selectedTaskId">
        Add Task to Sprint
      </button>
    </form>
  `,
})
export class SprintAddTaskFormComponent {
  private store = inject(Store);

  readonly sprintId = input.required<number>();

  readonly availableTasks = this.store.selectSignal(selectAvailableSprintTasks);
  readonly updateLoading = this.store.selectSignal(selectSprintUpdateLoading);

  selectedTaskId?: number;

  taskLabel = (task: TaskViewModel) => `${task.systemId} · ${task.name}`;
  taskValue = (task: TaskViewModel) => task.id;

  constructor() {
    effect(() => {
      if (
        this.selectedTaskId &&
        !this.availableTasks().some((t) => t.id === this.selectedTaskId)
      ) {
        this.selectedTaskId = undefined;
      }
    });
  }

  onSubmit(event: Event) {
    event.preventDefault();
    const sprintId = this.sprintId();
    if (!sprintId || !this.selectedTaskId) return;

    this.store.dispatch(
      addTasksToSprint({
        sprintId,
        request: { taskIds: [this.selectedTaskId] },
      })
    );
    this.selectedTaskId = undefined;
  }
}
