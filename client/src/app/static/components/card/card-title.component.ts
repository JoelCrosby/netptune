import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-title',
  template: '<ng-content/>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardTitleComponent {
  @HostBinding('class') className =
    'justify-between mb-[12px] text-2xl font-medium font-overpass text-foreground flex flex-row justify-between';
}
