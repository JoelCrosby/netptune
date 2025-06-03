import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
    selector: 'app-card-header',
    template: `
    <ng-content name="app-card-title" />
    <ng-content name="app-card-subtitle" />

    <ng-content />
  `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class CardHeaderComponent {
  @HostBinding('class') className = 'netp-card-header';
}
