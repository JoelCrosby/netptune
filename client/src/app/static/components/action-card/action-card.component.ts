import {
  booleanAttribute,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { LucideChevronRight } from '@lucide/angular';
import { cn } from '../button/button.variants';

export type ActionCardVariant = 'filled' | 'outlined' | 'accent';

const SURFACES: Record<ActionCardVariant, string> = {
  filled: 'bg-hover hover:bg-foreground/10',
  outlined: 'bg-card border-border hover:border-primary/60 border',
  accent: 'bg-primary/5 border-primary/30 hover:border-primary/60 border',
};

@Component({
  selector: 'app-action-card',
  host: { class: 'block' },
  imports: [LucideChevronRight],
  template: `
    <div [class]="surfaceClass()">
      <button
        type="button"
        class="focus-visible:ring-primary absolute inset-0 rounded-xl focus-visible:ring-2 focus-visible:outline-none"
        [attr.aria-label]="accessibleLabel()"
        (click)="activated.emit()"></button>

      <span
        class="text-muted pointer-events-none mt-0.5 flex h-4 w-4 shrink-0 items-center justify-center">
        <ng-content select="[actionCardIcon]" />
      </span>

      <span class="pointer-events-none min-w-0 flex-1">
        <span class="block truncate text-sm font-medium">{{ heading() }}</span>
        <span class="text-muted block text-xs"><ng-content /></span>
      </span>

      <span
        class="pointer-events-none relative flex shrink-0 items-start gap-2">
        <ng-content select="[actionCardTrailing]" />
      </span>

      @if (showChevron()) {
        <svg
          lucideChevronRight
          class="text-muted pointer-events-none mt-1.5 h-4 w-4 shrink-0"></svg>
      }
    </div>
  `,
})
export class ActionCardComponent {
  readonly heading = input.required<string>();
  readonly ariaLabel = input<string | null>(null);
  readonly showChevron = input(true, { transform: booleanAttribute });
  readonly variant = input<ActionCardVariant>('filled');

  readonly activated = output();

  protected readonly surfaceClass = computed(() => {
    return cn(
      'relative flex h-full items-start gap-3 rounded p-5 transition-colors',
      SURFACES[this.variant()]
    );
  });

  protected readonly accessibleLabel = computed(() => {
    return this.ariaLabel() ?? this.heading();
  });
}
