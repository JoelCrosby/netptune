import {
  ChangeDetectionStrategy,
  Component,
  inject,
  model,
} from '@angular/core';
import { editProjectTask } from '@app/core/store/tasks/tasks.actions';
import {
  selectDetailTask,
  selectDetailTaskIsRedOnly,
} from '@app/core/store/tasks/tasks.selectors';
import { selectCurrentHubGroupId } from '@core/store/hub-context/hub-context.selectors';
import { Store } from '@ngrx/store';
import { EditorComponent } from '@static/components/editor/editor.component';

@Component({
  selector: 'app-task-detail-description',
  template: ` <label class="font-sm font-semibold" for="description">
      Description
    </label>

    <app-editor
      class="border-foreground/30 mt-2 flex max-h-[calc(100vh-960px)] overflow-y-auto rounded-sm border-2 px-4 py-1"
      aria-labelledby="description"
      placeholder="Add a Description..."
      (saved)="updateTask($event)"
      [(value)]="description"
      [isReadOnly]="isReadOnly()">
    </app-editor>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EditorComponent],
})
export class TaskDetailDescriptionComponent {
  readonly store = inject(Store);

  task = this.store.selectSignal(selectDetailTask);
  hubGroupId = this.store.selectSignal(selectCurrentHubGroupId);
  isReadOnly = this.store.selectSignal(selectDetailTaskIsRedOnly);

  description = model(this.task()?.description ?? '');

  updateTask(value?: string) {
    if (typeof value === 'undefined' || value === null) {
      return;
    }

    const identifier = this.hubGroupId();
    const task = this.task();

    if (!identifier || !task) {
      return;
    }

    if (task.description === value) {
      return;
    }

    this.store.dispatch(
      editProjectTask({
        identifier,
        task: {
          ...task,
          description: value,
        },
      })
    );
  }
}
