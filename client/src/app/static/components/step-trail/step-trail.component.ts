import { Component, input, signal } from '@angular/core';
import { LucideChevronDown } from '@lucide/angular';

export interface StepTrailItem {
  label: string;
  failed: boolean;
}

@Component({
  selector: 'app-step-trail',
  imports: [LucideChevronDown],
  host: { class: 'flex flex-col gap-1.5' },
  template: `
    <button
      type="button"
      class="text-muted hover:text-foreground focus-visible:ring-primary flex items-center gap-1.5 self-start rounded text-left text-xs transition-colors focus-visible:ring-2 focus-visible:outline-none"
      [attr.aria-expanded]="expanded()"
      (click)="expanded.set(!expanded())">
      <svg
        lucideChevronDown
        class="h-3.5 w-3.5 shrink-0 transition-transform duration-150"
        [class.-rotate-90]="!expanded()"
        aria-hidden="true"></svg>
      <span>{{ summary() }}</span>
    </button>

    @if (expanded()) {
      <ol class="border-border ml-1.5 flex flex-col gap-1 border-l pl-3">
        @for (step of steps(); track $index) {
          <li
            class="font-avatar truncate text-[11.5px]"
            [class]="step.failed ? 'text-warn' : 'text-muted'">
            {{ step.label }}
            @if (step.failed) {
              <span i18n="Marks a step in a list of steps that failed"
                >(failed)</span
              >
            }
          </li>
        }
      </ol>
    }
  `,
})
export class StepTrailComponent {
  readonly summary = input.required<string>();
  readonly steps = input.required<StepTrailItem[]>();

  readonly expanded = signal(false);
}
