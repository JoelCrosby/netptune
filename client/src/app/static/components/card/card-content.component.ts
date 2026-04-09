import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-content',
  template: '<ng-content/>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardContentComponent {
  @HostBinding('class') className = 'flex flex-col gap-4';
}
