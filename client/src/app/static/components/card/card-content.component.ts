import { Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-card-content',
  template: '<ng-content/>',
})
export class CardContentComponent {
  @HostBinding('class') className = 'flex flex-col gap-4';
}
