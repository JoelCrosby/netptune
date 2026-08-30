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
import { InlineEditHeadingComponent } from '@app/static/components/inline-edit-heading/inline-edit-heading.component';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-header',
  template: `
    <app-inline-edit-heading
      [textClass]="textClass()"
      (submitted)="updateTask(this.name())"
      [(value)]="name"
      [isReadonly]="isReadOnly()" />
  `,
  imports: [InlineEditHeadingComponent],
})
export class TaskDetailHeaderComponent {
  readonly textClass = input(
    '-mx-2 px-2 py-1 text-[28px]/[36px] font-semibold tracking-[-0.012em]'
  );

  private readonly taskDetail = inject(TaskDetailService);

  task = this.taskDetail.task;
  private readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);

  isReadOnly = computed(() => !this.canUpdate());

  name = model(this.task()?.name ?? '');

  constructor() {
    effect(() => {
      this.name.set(this.task()?.name ?? '');
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

    if (task.name === value) {
      return;
    }

    this.taskDetail.updateTask({ name: value });
  }
}
