import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-task-date',
  imports: [DatePipe, TooltipDirective],
  template: `
    @if (value(); as date) {
      <span
        class="text-muted text-sm whitespace-nowrap"
        [appTooltip]="date | date: 'medium'">
        {{ date | date: 'mediumDate' }}
      </span>
    }
  `,
})
export class TaskDateComponent {
  readonly value = input<string | Date | null | undefined>(null);
}
