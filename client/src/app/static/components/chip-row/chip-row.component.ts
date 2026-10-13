import { Component, input } from '@angular/core';

@Component({
  selector: 'app-chip-row',
  host: { class: 'flex flex-wrap items-center gap-2.5' },
  template: `
    @if (label()) {
      <span
        class="text-muted mr-1 text-[11px] font-bold tracking-[.07em] uppercase">
        {{ label() }}
      </span>
    }
    <ng-content />
  `,
})
export class ChipRowComponent {
  readonly label = input('');
}
