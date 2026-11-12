import { Component, input, model } from '@angular/core';
import { LucideChevronDown } from '@lucide/angular';

// A titled group that folds its content away — a chevron, the label, a count
// pill, a rule running to an optional trailing note.
@Component({
  selector: 'app-collapsible-group',
  imports: [LucideChevronDown],
  host: { class: 'flex flex-col gap-3' },
  template: `
    <h2 class="flex">
      <button
        type="button"
        class="focus-visible:ring-primary flex min-w-0 flex-1 cursor-pointer items-center gap-2.5 rounded py-1 text-left focus-visible:ring-2 focus-visible:outline-none"
        [attr.aria-expanded]="expanded()"
        (click)="expanded.set(!expanded())">
        <svg
          lucideChevronDown
          class="text-muted h-3.75 w-3.75 shrink-0 transition-transform duration-150"
          [class.-rotate-90]="!expanded()"
          aria-hidden="true"></svg>

        <span class="truncate text-[15px] font-semibold">{{ label() }}</span>

        @if (count() !== null) {
          <span
            class="bg-foreground/10 text-muted inline-flex h-5 shrink-0 items-center rounded-full px-1.75 text-[11.5px] font-semibold">
            {{ count() }}
          </span>
        }

        <span class="bg-border h-px min-w-4 flex-1"></span>

        @if (meta()) {
          <span class="text-muted shrink-0 text-xs">{{ meta() }}</span>
        }
      </button>
    </h2>

    @if (expanded()) {
      <ng-content />
    }
  `,
})
export class CollapsibleGroupComponent {
  readonly label = input.required<string>();
  readonly count = input<number | null>(null);
  readonly meta = input<string | null>(null);

  readonly expanded = model(true);
}
