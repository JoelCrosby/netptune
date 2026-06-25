import { Component, input } from '@angular/core';
import { LucideChevronDown } from '@lucide/angular';
import { SpinnerComponent } from '../spinner/spinner.component';

@Component({
  selector: 'app-chip-option, button[app-chip-option]',
  template: `
    <ng-content />

    @if (loading()) {
      <app-spinner class="ml-2" diameter="0.75rem" />
    } @else {
      @if (showChevron()) {
        <svg lucideChevronDown class="ml-2 h-4 w-4"></svg>
      }
    }
  `,
  imports: [LucideChevronDown, SpinnerComponent],
  host: {
    class:
      'inline-flex items-center px-3 h-7 rounded-full text-xs font-medium bg-card-selected/40 text-foreground select-none transition-colors hover:bg-card-selected/80 cursor-default',
  },
})
export class ChipOptionComponent {
  readonly showChevron = input(true);
  readonly loading = input(false);
}
