import {
  ChangeDetectionStrategy,
  Component,
  inject,
  model,
} from '@angular/core';
import { selectCurrentHubGroupId } from '@app/core/store/hub-context/hub-context.selectors';
import { editProjectTask } from '@app/core/store/tasks/tasks.actions';
import {
  selectDetailTask,
  selectDetailTaskIsRedOnly,
} from '@app/core/store/tasks/tasks.selectors';
import { InlineEditHeadingComponent } from '@app/static/components/inline-edit-heading/inline-edit-heading.component';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-task-detail-header',
  template: `
    <app-inline-edit-heading
      (submitted)="updateTask(this.name())"
      [(value)]="name"
      [isReadonly]="isReadOnly()" />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [InlineEditHeadingComponent],
})
export class TaskDetailHeaderComponent {
  readonly store = inject(Store);

  task = this.store.selectSignal(selectDetailTask);
  hubGroupId = this.store.selectSignal(selectCurrentHubGroupId);
  isReadOnly = this.store.selectSignal(selectDetailTaskIsRedOnly);

  name = model(this.task()?.name ?? '');

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

    this.store.dispatch(
      editProjectTask({
        identifier,
        task: {
          ...task,
          name: value,
        },
      })
    );
  }
}
