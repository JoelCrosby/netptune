import { Component, computed, input, output } from '@angular/core';
import { statusResource } from '@core/resources/status.resource';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';

@Component({
  selector: 'app-ai-assistant-status-picker',
  imports: [FormSelectComponent, FormSelectOptionComponent],
  template: `
    <app-form-select
      [label]="label()"
      [noMargin]="true"
      [value]="value()"
      (changed)="valueChange.emit($event)">
      @if (isClearable()) {
        <app-form-select-option [value]="null">
          <span i18n="Option that uses the project default">
            Project default
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
export class AiAssistantStatusPickerComponent {
  readonly label = input.required<string>();
  readonly value = input<string | null>(null);
  readonly isClearable = input(false);

  readonly valueChange = output<string | null>();

  private readonly statuses = statusResource();

  protected readonly options = computed(() => {
    return this.statuses.value().map((status) => {
      return { id: String(status.id), name: status.name };
    });
  });
}
