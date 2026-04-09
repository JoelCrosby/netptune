import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-group',
  template: '<ng-content/>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardGroupComponent {
  @HostBinding('class') className =
    'inline-flex flex-wrap gap-4 p-2 bg-board-group mb-2';
}
