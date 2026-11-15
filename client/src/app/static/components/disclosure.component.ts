import { Component, input, linkedSignal } from '@angular/core';
import { LucideChevronDown } from '@lucide/angular';
import { SpinnerIconComponent } from './spinner/spinner-icon.component';

let nextId = 0;

@Component({
  selector: 'app-disclosure',
  imports: [LucideChevronDown, SpinnerIconComponent],
  host: { class: 'flex min-h-0 min-w-0 flex-col gap-1' },
  template: `
    <button
      type="button"
      class="text-muted hover:text-foreground focus-visible:ring-primary flex min-w-0 items-center gap-1.5 self-start rounded text-left text-xs transition-colors focus-visible:ring-2 focus-visible:outline-none"
      [attr.aria-expanded]="expanded()"
      [attr.aria-controls]="expanded() ? panelId : null"
      (click)="expanded.set(!expanded())">
      @if (busy()) {
        <app-spinner-icon class="h-3.5 w-3.5 shrink-0" />
      }
      <svg
        lucideChevronDown
        class="h-3.5 w-3.5 shrink-0 transition-transform duration-150"
        [class.-rotate-90]="!expanded()"
        aria-hidden="true"></svg>
      <span class="truncate">{{ label() }}</span>
      @if (meta()) {
        <span class="ml-auto shrink-0 pl-2 tabular-nums">{{ meta() }}</span>
      }
    </button>

    @if (busy()) {
      <p class="sr-only" aria-live="polite">{{ label() }}</p>
    }

    @if (expanded()) {
      <div
        class="border-border ml-1.5 flex min-h-0 min-w-0 flex-col gap-1.5 border-l pl-3"
        [id]="panelId">
        <ng-content />
      </div>
    }
  `,
})
export class DisclosureComponent {
  readonly label = input.required<string>();
  readonly meta = input('');
  readonly busy = input(false);

  // Something still happening shows itself; something finished stays out of the
  // way. Collapsing it by hand sticks until it is no longer busy.
  readonly expanded = linkedSignal(() => this.busy());

  protected readonly panelId = `disclosure-panel-${nextId++}`;
}
