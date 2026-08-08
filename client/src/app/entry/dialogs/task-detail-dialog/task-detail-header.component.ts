import { Component, computed, effect, inject, model } from '@angular/core';
import { selectCurrentHubGroupId } from '@app/core/store/hub-context/hub-context.selectors';
import { InlineEditHeadingComponent } from '@app/static/components/inline-edit-heading/inline-edit-heading.component';
import { Store } from '@ngrx/store';
import { TaskDetailService } from './task-detail.service';
import { selectCanUpdateTask } from '@app/core/store/permissions/permissions.selectors';

@Component({
  selector: 'app-task-detail-header',
  template: `
    <app-inline-edit-heading
      (submitted)="updateTask(this.name())"
      [(value)]="name"
      [isReadonly]="isReadOnly()" />
  `,
  imports: [InlineEditHeadingComponent],
})
export class TaskDetailHeaderComponent {
  readonly store = inject(Store);

  private readonly taskDetail = inject(TaskDetailService);

  task = this.taskDetail.task;
  hubGroupId = this.store.selectSignal(selectCurrentHubGroupId);
  private readonly canUpdate = selectCanUpdateTask(this.store);

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

    const identifier = this.hubGroupId();
    const task = this.task();

    if (!identifier || !task) {
      return;
    }

    if (task.name === value) {
      return;
    }

    this.taskDetail.updateTask({ name: value });
  }
}
