import { Component, input } from '@angular/core';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { AvatarStackComponent } from './avatar-stack/avatar-stack.component';

@Component({
  selector: 'app-task-assignees',
  imports: [AvatarStackComponent],
  template: `
    @if (assignees().length) {
      <app-avatar-stack [avatars]="assignees()" />
    } @else {
      <span
        class="text-muted text-sm"
        i18n="Shown when a task has nobody assigned">
        Unassigned
      </span>
    }
  `,
})
export class TaskAssigneesComponent {
  readonly assignees = input<readonly AssigneeViewModel[]>([]);
}
