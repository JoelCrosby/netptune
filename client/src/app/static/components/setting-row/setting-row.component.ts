import { Component, input } from '@angular/core';

/** One labelled setting inside a panel, with its controls projected on the right. */
@Component({
  selector: 'app-setting-row',
  host: {
    class:
      'border-border flex items-center justify-between gap-4 border-b px-6 py-4 last:border-b-0',
  },
  template: `
    <div class="min-w-0">
      <p class="truncate text-sm font-medium">{{ label() }}</p>
      @if (hint()) {
        <p class="text-muted text-xs">{{ hint() }}</p>
      }
    </div>

    <div class="flex shrink-0 items-center gap-3">
      <ng-content />
    </div>
  `,
})
export class SettingRowComponent {
  readonly label = input.required<string>();
  readonly hint = input('');
}
