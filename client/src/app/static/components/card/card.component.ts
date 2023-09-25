import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card',
  template: `
    <ng-content name="app-card-header-image"/>
    <ng-content name="app-card-header"/>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardComponent {
  @HostBinding('class') className = 'netp-card';
}
