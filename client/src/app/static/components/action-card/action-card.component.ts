import {
  booleanAttribute,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { LucideChevronRight } from '@lucide/angular';

/**
 * A row that reads as a card: leading icon, heading, description and a chevron.
 * The click target covers the whole card, so anything projected into the
 * trailing slot is inert until it opts back in with `pointer-events-auto`.
 */
@Component({
  selector: 'app-action-card',
  host: { class: 'block' },
  imports: [LucideChevronRight],
  template: `
    <div
      class="bg-hover hover:bg-foreground/10 relative flex items-start gap-3 rounded-xl px-4 py-3 transition-colors">
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

  readonly activated = output();

  protected readonly accessibleLabel = computed(() => {
    return this.ariaLabel() ?? this.heading();
  });
}
