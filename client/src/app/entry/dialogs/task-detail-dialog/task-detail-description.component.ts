import {
  Component,
  computed,
  effect,
  inject,
  input,
  model,
} from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import { hasPermission } from '@core/auth/has-permission';
import { TaskViewModel } from '@app/core/models/view-models/project-task-dto';
import {
  EditorAppearance,
  EditorComponent,
} from '@static/components/editor/editor.component';
import { EYEBROW } from './task-detail-styles';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-description',
  template: `
    @if (label(); as label) {
      <div [class]="eyebrowClass" [id]="labelId">{{ label }}</div>
    }

    <app-editor
      [attr.aria-labelledby]="label() ? labelId : null"
      i18n-placeholder="Placeholder in the empty task description editor"
      placeholder="Add a Description..."
      [appearance]="appearance()"
      [hostClass]="textClass()"
      (saved)="updateTask($event)"
      [finalSave]="finalSave()"
      [(value)]="description"
      [isReadOnly]="isReadOnly()"></app-editor>
  `,
  imports: [EditorComponent],
})
export class TaskDetailDescriptionComponent {
  readonly appearance = input<EditorAppearance>('flat');
  readonly textClass = input('text-[15px]/[26px]');
  readonly label = input<string | null>(null);

  private readonly taskDetail = inject(TaskDetailService);

  readonly task = this.taskDetail.task;
  private readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);

  readonly eyebrowClass = `${EYEBROW} mb-2.5`;
  readonly labelId = 'task-detail-description-label';

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
