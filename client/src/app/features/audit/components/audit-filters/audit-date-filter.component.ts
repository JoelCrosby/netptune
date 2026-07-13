import { Component, input, model } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-audit-date-filter',
  imports: [FormsModule],
  host: {
    class: 'flex flex-col gap-1',
  },
  template: `
    <label
      class="text-foreground/60 text-xs font-medium tracking-wide uppercase"
      [for]="controlId()">
      {{ label() }}
    </label>
    <input
      class="bg-background border-border h-10 rounded-sm border px-3 py-1.5 text-sm"
      type="date"
      [id]="controlId()"
      [ngModel]="value()"
      (ngModelChange)="value.set($event)" />
  `,
})
export class AuditDateFilterComponent {
  readonly controlId = input.required<string>();
  readonly label = input.required<string>();
  readonly value = model('');
}
