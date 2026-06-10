import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-header',
  template: `
    <div class="items-basline flex w-full flex-col gap-1">
      <div class="flex items-center justify-between">
        <ng-content select="app-card-title" />
        <ng-content select="app-card-delete" />
      </div>

      <ng-content select="app-card-subtitle" />
    </div>

    <ng-content />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardHeaderComponent {
  @HostBinding('class') className = 'flex flex-col items-start';
}
