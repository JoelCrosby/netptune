import { Component, input } from '@angular/core';

let nextId = 0;

@Component({
  selector: 'app-chip-row',
  host: {
    class: 'flex flex-wrap items-center gap-2.5',
    '[attr.role]': "label() ? 'group' : null",
    '[attr.aria-labelledby]': 'label() ? labelId : null',
  },
  template: `
    @if (label()) {
      <span
        [id]="labelId"
        class="text-muted mr-1 text-[11px] font-bold tracking-[.07em] uppercase">
        {{ label() }}
      </span>
    }
    <ng-content />
  `,
})
export class ChipRowComponent {
  readonly label = input('');

  protected readonly labelId = `chip-row-label-${nextId++}`;
}
