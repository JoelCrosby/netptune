import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
    selector: 'app-card-subtitle',
    template: '<ng-content/>',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class CardSubtitleComponent {
  @HostBinding('class') className = 'netp-card-subtitle';
}
