import { Component, computed, input } from '@angular/core';
import { cn } from '@static/components/button/button.variants';

export type AutomationStateTone = 'success' | 'neutral' | 'warn';

const toneClasses: Record<AutomationStateTone, string> = {
  success: 'bg-green-500/10 text-green-700 dark:text-green-400',
  neutral: 'bg-foreground/5 text-foreground/60',
  warn: 'bg-warn/8 text-warn',
};

@Component({
  selector: 'app-automation-state-pill',
  host: { '[class]': 'hostClass()' },
  template: `
    <span class="h-1.5 w-1.5 rounded-full bg-current" aria-hidden="true"></span>
    <ng-content />
  `,
})
export class AutomationStatePillComponent {
  readonly tone = input<AutomationStateTone>('neutral');

  protected readonly hostClass = computed(() => {
    return cn(
      'inline-flex h-6 items-center gap-1.5 rounded-full px-2.25 text-xs font-semibold whitespace-nowrap',
      toneClasses[this.tone()]
    );
  });
}
