import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-title',
  template: '<ng-content/>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardTitleComponent {
  @HostBinding('class') className =
    'flex flex-row justify-between mb-[12px] text-[20px] font-medium font-overpass text-neutral-900 dark:text-white';
}
