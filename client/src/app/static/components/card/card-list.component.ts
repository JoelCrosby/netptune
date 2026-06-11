import { Component } from '@angular/core';

@Component({
  selector: 'app-card-list',
  template: `
    <div class="flex flex-col gap-3 rounded-sm p-[.6rem]">
      <ng-content />
    </div>
  `,
})
export class CardListComponent {}
