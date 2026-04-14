import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-list-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <div
      class="bg-card mb-0.75 flex h-10 items-center overflow-hidden rounded-sm transition-colors duration-200 ease-in-out">
      <ng-content />
    </div>
  `,
})
export class ListItemComponent {}
