import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import { selectCanUpdateTask } from '@app/core/store/permissions/permissions.selectors';
import { selectTagNames } from '@app/core/store/tags/tags.selectors';
import {
  addTagToTask,
  deleteTagFromTask,
} from '@app/core/store/tasks/tasks.actions';
import { selectDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { FormSelectTagsOptionComponent } from '@app/static/components/form-select-tags/form-select-tags-option.component';
import { FormSelectTagsComponent } from '@app/static/components/form-select-tags/form-select-tags.component';
import { selectCurrentHubGroupId } from '@core/store/hub-context/hub-context.selectors';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-task-detail-tags',
  template: `
    <h4 class="font-sm mt-4 mb-2 font-semibold">Tags</h4>
    <app-form-select-tags
      class="tags-autocomplete"
      placeholder="Add a Tag..."
      [value]="selectedTags()"
      (changed)="onTagsSelectionChanged($event)"
      [isReadonly]="!canUpdate()">
      @for (tag of tags(); track tag) {
        <app-form-select-tags-option [value]="tag">
          {{ tag }}
        </app-form-select-tags-option>
      }
    </app-form-select-tags>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormSelectTagsComponent, FormSelectTagsOptionComponent],
})
export class TaskDetailTagsComponent {
  readonly store = inject(Store);

  tags = this.store.selectSignal(selectTagNames);

  task = this.store.selectSignal(selectDetailTask);
  hubGroupId = this.store.selectSignal(selectCurrentHubGroupId);
  selectedTags = computed(() => this.task()?.tags ?? []);

  canUpdate = selectCanUpdateTask(this.store);

  onTagsSelectionChanged(tags: string[]) {
    const task = this.task();
    const identifier = this.hubGroupId();

    if (!task || !identifier) return;

    const currentTags = new Set(task.tags);
    const nextTags = new Set(tags);

    const added = tags.find((t) => !currentTags.has(t));
    const removed = task.tags.find((t) => !nextTags.has(t));

    if (removed) {
      this.store.dispatch(
        deleteTagFromTask({
          identifier,
          systemId: task.systemId,
          tag: removed,
        })
      );
    } else if (added) {
      this.store.dispatch(
        addTagToTask({
          identifier,
          request: {
            systemId: task.systemId,
            tag: added,
          },
        })
      );
    }
  }
}
