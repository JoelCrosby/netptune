import { Component, input, output } from '@angular/core';
import { workspaceUsersResource } from '@core/resources/user.resource';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectTagsOptionComponent } from '@static/components/form-select-tags/form-select-tags-option.component';
import { FormSelectTagsComponent } from '@static/components/form-select-tags/form-select-tags.component';

@Component({
  selector: 'app-ai-assistant-member-picker',
  imports: [
    FormSelectComponent,
    FormSelectOptionComponent,
    FormSelectTagsComponent,
    FormSelectTagsOptionComponent,
  ],
  template: `
    @if (isMultiple()) {
      <app-form-select-tags
        [label]="label()"
        [value]="values()"
        (changed)="valuesChange.emit($event)">
        @for (member of members(); track member.id) {
          <app-form-select-tags-option [value]="member.id">
            {{ member.displayName }}
          </app-form-select-tags-option>
        }
      </app-form-select-tags>
    } @else {
      <app-form-select
        [label]="label()"
        [noMargin]="true"
        [value]="value()"
        (changed)="valueChange.emit($event)">
        @if (isClearable()) {
          <app-form-select-option [value]="null">
            <span i18n="Option that leaves a task without an assignee">
              Unassigned
            </span>
          </app-form-select-option>
        }
        @for (member of members(); track member.id) {
          <app-form-select-option [value]="member.id">
            {{ member.displayName }}
          </app-form-select-option>
        }
      </app-form-select>
    }
  `,
})
export class AiAssistantMemberPickerComponent {
  readonly label = input.required<string>();
  readonly value = input<string | null>(null);
  readonly values = input<string[]>([]);
  readonly isMultiple = input(false);
  readonly isClearable = input(false);

  readonly valueChange = output<string | null>();
  readonly valuesChange = output<string[]>();

  protected readonly members = workspaceUsersResource();
}
