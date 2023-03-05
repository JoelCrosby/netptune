import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-header',
  template: `
    <ng-content name="app-card-title"></ng-content>
    <ng-content name="app-card-subtitle"></ng-content>

    <ng-content></ng-content>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardHeaderComponent {
  @HostBinding('class') className = 'netp-card-header';
}
