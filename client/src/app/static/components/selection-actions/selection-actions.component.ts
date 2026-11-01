import { Component, input } from '@angular/core';

@Component({
  selector: 'app-selection-actions',
  host: { class: 'flex flex-row items-center gap-4' },
  template: `
    <span
      class="text-muted px-2 text-sm"
      i18n="Count of selected rows. COUNT is the number selected">
      {{
        count() // i18n(ph="COUNT")
      }}
      selected
    </span>
    <ng-content />
  `,
})
export class SelectionActionsComponent {
  readonly count = input.required<number>();
}
