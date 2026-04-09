import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-card-list',
  template: `
    <div class="card-group">
      <ng-content />
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardListComponent {}
