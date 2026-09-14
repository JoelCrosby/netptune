import { Component, computed, input, output } from '@angular/core';
import {
  UserSelectOption,
  UserSelectValue,
} from '@core/models/view-models/user-select-option';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { fieldRowClass } from '@static/components/field-row/field-row.component';
import { UserSelectComponent } from '@static/components/user-select/user-select.component';
import { TaskFieldBase } from './field-label';

@Component({
  selector: 'app-task-assignee-field',
  imports: [AvatarComponent, UserSelectComponent],
  host: { class: 'block' },
  template: `
    <app-user-select
      [buttonClass]="rowClass"
      [disabled]="disabled()"
      [excludeServiceAccounts]="true"
      [value]="assignees()"
      (selectChange)="toggled.emit($event)">
      <span [class]="labelClass()">{{ labels.assignee }}</span>
      @if (assignees().length) {
        <span class="flex min-w-0 items-center gap-1.5 font-medium">
          @for (assignee of assignees(); track assignee.id) {
            <app-avatar
              size="sm"
              [tooltip]="true"
              [name]="assignee.displayName"
              [imageUrl]="assignee.pictureUrl"
              [isServiceAccount]="assignee.isServiceAccount ?? false" />
          }
          <span class="truncate">{{ assigneeLabel() }}</span>
        </span>
      } @else {
        <span class="text-muted">{{ labels.unassigned }}</span>
      }
    </app-user-select>
  `,
})
export class TaskAssigneeFieldComponent extends TaskFieldBase {
  readonly assignees = input.required<UserSelectValue[]>();

  readonly toggled = output<UserSelectOption>();

  protected readonly rowClass = fieldRowClass;

  protected readonly labels = {
    assignee: $localize`:Field heading for the people a task is assigned to:Assignee`,
    unassigned: $localize`:Shown in the assignee picker when a task has nobody assigned:Unassigned`,
  };

  protected readonly assigneeLabel = computed(() => {
    const assignees = this.assignees();

    if (assignees.length === 1) return assignees[0].displayName;

    return `${assignees.length}`;
  });
}
