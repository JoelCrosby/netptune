import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
    selector: 'app-card-actions',
    template: '<ng-content/>',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class CardActionsComponent {
  @HostBinding('class') className = 'netp-card-actions';
}
