import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-card-list',
  template: `
    <div class="bg-board-group flex flex-col gap-3 rounded-[.4rem] p-[.6rem]">
      <ng-content />
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardListComponent {}
