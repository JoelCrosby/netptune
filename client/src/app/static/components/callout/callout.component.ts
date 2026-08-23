import { Component, computed, input } from '@angular/core';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { cva } from 'class-variance-authority';
import { cn } from '../button/button.variants';

export type CalloutColor = 'primary' | 'warn' | 'neutral';

const calloutVariants = cva(
  'flex items-start gap-2 rounded-md border px-3 py-2 text-sm',
  {
    variants: {
      color: {
        primary: 'border-primary/40 bg-primary/5',
        warn: 'border-warn/40 bg-warn/5',
        neutral: 'border-border bg-foreground/5',
      },
    },
    defaultVariants: {
      color: 'neutral',
    },
  }
);

const iconColors: Record<CalloutColor, string> = {
  primary: 'text-primary',
  warn: 'text-warn',
  neutral: 'text-foreground/60',
};

/**
 * Inline message attached to the content it is about, as opposed to `app-banner`, which is a
 * page-level toast owned by a service.
 */
@Component({
  selector: 'app-callout',
  imports: [LucideDynamicIcon],
  host: { class: 'block' },
  template: `
    <div [class]="hostClass()" [attr.role]="role()">
      @if (icon(); as icon) {
        <svg [lucideIcon]="icon" [class]="iconClass()" aria-hidden="true"></svg>
      }

      <div class="min-w-0 flex-1">
        @if (title(); as title) {
          <p class="font-medium">{{ title }}</p>
        }
        <ng-content />
      </div>
    </div>
  `,
})
export class CalloutComponent {
  readonly color = input<CalloutColor>('neutral');
  readonly icon = input<LucideIconInput>();
  readonly title = input<string>();
  readonly role = input('status');
  readonly class = input('');

  protected readonly hostClass = computed(() =>
    cn(calloutVariants({ color: this.color() }), this.class())
  );

  protected readonly iconClass = computed(() =>
    cn('mt-0.5 h-4 w-4 shrink-0', iconColors[this.color()])
  );
}
