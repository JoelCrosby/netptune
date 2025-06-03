import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
    selector: 'app-card-title',
    template: '<ng-content/>',
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class CardTitleComponent {
  @HostBinding('class') className = 'netp-card-title';
}
