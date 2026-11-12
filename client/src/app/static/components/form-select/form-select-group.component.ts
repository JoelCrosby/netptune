import { Component, input } from '@angular/core';

@Component({
  selector: 'app-form-select-group',
  host: {
    class: 'flex shrink-0 flex-col gap-[.4rem] not-first:mt-1.5',
    role: 'group',
    '[attr.aria-label]': 'label()',
  },
  template: `
    <div
      class="text-muted px-[.4rem] pt-1 text-[11px] font-semibold tracking-[.04em] uppercase"
      aria-hidden="true">
      {{ label() }}
    </div>
    <ng-content />
  `,
})
export class FormSelectGroupComponent {
  readonly label = input.required<string>();
}
