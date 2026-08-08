import { Component, computed, effect, inject, model } from '@angular/core';
import { TaskViewModel } from '@app/core/models/view-models/project-task-dto';
import { Store } from '@ngrx/store';
import { EditorComponent } from '@static/components/editor/editor.component';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { TaskDetailService } from './task-detail.service';
import { selectCanUpdateTask } from '@app/core/store/permissions/permissions.selectors';

@Component({
  selector: 'app-task-detail-description',
  template: `
    <label class="font-sm font-semibold" for="description">
      <span i18n="Label of the task description editor">Description</span>
    </label>

    <app-editor
      aria-labelledby="description"
      i18n-placeholder="Placeholder in the empty task description editor"
      placeholder="Add a Description..."
      (saved)="updateTask($event)"
      [finalSave]="finalSave()"
      [(value)]="description"
      [isReadOnly]="isReadOnly()"
      class="@xl:px-16"></app-editor>
  `,
  host: { class: '@container' },
  imports: [EditorComponent],
})
export class TaskDetailDescriptionComponent {
  readonly store = inject(Store);

  private readonly taskDetail = inject(TaskDetailService);

  task = this.taskDetail.task;
  readonly hubGroupId = inject(ProjectTasksHubService).currentGroupId;
  private readonly canUpdate = selectCanUpdateTask(this.store);

  isReadOnly = computed(() => !this.canUpdate());
  description = model(this.task()?.description ?? '');

  finalSave = computed(() => {
    const task = this.task();

    if (!task) return null;

    return (value: string) => this.saveDescription(task, value);
  });

  constructor() {
    effect(() => {
      this.description.set(this.task()?.description ?? '');
    });
  }

  updateTask(value?: string) {
    if (typeof value === 'undefined' || value === null) {
      return;
    }

    const task = this.task();

    if (!task) {
      return;
    }

    this.saveDescription(task, value);
  }

  private saveDescription(task: TaskViewModel, description: string) {
    if (task.description === description) {
      return;
    }

    this.taskDetail.updateTask({ description });
  }
}
