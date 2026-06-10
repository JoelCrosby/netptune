import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-card-delete',
  template: ` <ng-content />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: '',
  },
})
export class CardDeleteComponent {}
