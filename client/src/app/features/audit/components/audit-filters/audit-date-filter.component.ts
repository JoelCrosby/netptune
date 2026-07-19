import { Component, input, model } from '@angular/core';
import { DatePickerComponent } from '@static/components/date-picker/date-picker.component';

@Component({
  selector: 'app-audit-date-filter',
  imports: [DatePickerComponent],
  host: {
    class: 'flex flex-col gap-1',
  },
  template: `
    <label
      class="text-foreground/60 text-xs font-medium tracking-wide uppercase"
      [for]="controlId()">
      {{ label() }}
    </label>
    <app-date-picker
      [controlId]="controlId()"
      [ariaLabel]="label()"
      [(value)]="value" />
  `,
})
export class AuditDateFilterComponent {
  readonly controlId = input.required<string>();
  readonly label = input.required<string>();
  readonly value = model('');
}
