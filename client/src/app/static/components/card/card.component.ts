import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card',
  template: `
    <ng-content name="app-card-header-image"></ng-content>
    <ng-content name="app-card-header"></ng-content>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardComponent {
  @HostBinding('class') className = 'netp-card';
}
