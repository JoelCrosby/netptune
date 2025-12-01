import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-header-image',
  template: '<ng-content/>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardHeaderImageComponent {
  @HostBinding('class') className = 'netp-card-header-image';
}
