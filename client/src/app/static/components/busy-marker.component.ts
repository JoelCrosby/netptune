import { Component, input } from '@angular/core';
import { LucideSparkles } from '@lucide/angular';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-busy-marker',
  imports: [LucideSparkles, TooltipDirective],
  host: { class: 'contents' },
  template: `
    <svg
      lucideSparkles
      class="text-primary h-4 w-4 shrink-0 animate-pulse motion-reduce:animate-none"
      role="img"
      [attr.aria-label]="label()"
      [appTooltip]="label()"></svg>
  `,
})
export class BusyMarkerComponent {
  readonly label = input.required<string>();
}
