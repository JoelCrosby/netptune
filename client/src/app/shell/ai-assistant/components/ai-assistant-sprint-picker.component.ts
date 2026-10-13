import { Component, computed, input, output } from '@angular/core';
import { sprintResource } from '@core/resources/sprint.resource';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';

@Component({
  selector: 'app-ai-assistant-sprint-picker',
  imports: [FormSelectComponent, FormSelectOptionComponent],
  template: `
    <app-form-select
      [label]="label()"
      [noMargin]="true"
      [value]="value()"
      (changed)="valueChange.emit($event)">
      @if (isClearable()) {
        <app-form-select-option [value]="null">
          <span i18n="Option placing the created task outside any sprint">
            Backlog
          </span>
        </app-form-select-option>
      }
      @for (option of options(); track option.id) {
        <app-form-select-option [value]="option.id">
          {{ option.name }}
        </app-form-select-option>
      }
    </app-form-select>
  `,
})
export class AiAssistantSprintPickerComponent {
  readonly label = input.required<string>();
  readonly value = input<string | null>(null);
  readonly projectId = input<number | null>(null);
  readonly isClearable = input(false);

  readonly valueChange = output<string | null>();

  private readonly sprints = sprintResource();

  protected readonly options = computed(() => {
    const projectId = this.projectId();
    const sprints = this.sprints.value().filter((sprint) => {
      return projectId === null || sprint.projectId === projectId;
    });

    return sprints.map((sprint) => {
      return { id: String(sprint.id), name: sprint.name };
    });
  });
}
