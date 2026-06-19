import { Component, computed, inject, input } from '@angular/core';
import { Selected } from '@core/models/selected';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { toggleSelectedAssignee } from '@core/store/tasks/tasks.actions';
import { selectTaskAssigneeOptions } from '@core/store/tasks/tasks.selectors';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';

@Component({
  selector: 'app-task-list-assignees',
  imports: [AvatarComponent],
  template: `
    @if (assignees().length) {
      <div class="inline-flex flex-row-reverse items-center">
        @for (
          assignee of assignees();
          track trackByAssignee($index, assignee)
        ) {
          <div
            class="bg-background inline-flex h-10 w-10 cursor-pointer items-center justify-center overflow-hidden rounded-full border-4 not-last:-ml-3 hover:z-100"
            [class.border-transparent]="!assignee.selected"
            [class.border-primary]="assignee.selected"
            [style.z-index]="assignee.selected ? 99 : null">
            <app-avatar
              size="lg"
              [name]="assignee.displayName"
              [imageUrl]="assignee.pictureUrl"
              (click)="onAssigneeClicked(assignee)" />
          </div>
        }
      </div>
    } @else {
      <div class="flex h-10 items-center">
        <div
          class="text-foreground/50 px-2 text-sm font-medium whitespace-nowrap select-none">
          No assignees
        </div>
      </div>
    }
  `,
})
export class TaskListAssigneesComponent {
  private readonly store = inject(Store);

  readonly assigneeOptions = input<Selected<AssigneeViewModel>[] | null>(null);
  readonly storeAssignees = this.store.selectSignal(selectTaskAssigneeOptions);
  readonly assignees = computed(
    () => this.assigneeOptions() ?? this.storeAssignees()
  );

  trackByAssignee(_: number, assignee: Selected<AssigneeViewModel>) {
    return assignee.id;
  }

  onAssigneeClicked(selected: Selected<AssigneeViewModel>) {
    this.store.dispatch(toggleSelectedAssignee({ assigneeId: selected.id }));
  }
}
