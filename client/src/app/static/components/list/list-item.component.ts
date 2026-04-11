import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-list-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <div
      class="mb-[3px] flex h-10 items-center overflow-hidden rounded-sm bg-card transition-colors duration-200 ease-in-out"
    >
      <ng-content />
    </div>
  `,
})
export class ListItemComponent {}
