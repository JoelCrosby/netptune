import { Component } from '@angular/core';

@Component({
  selector: 'app-card-title',
  template: `<h2>
    <ng-content />
  </h2>`,
  host: {
    class: 'flex text-xl font-medium font-overpass text-foreground',
  },
})
export class CardTitleComponent {}
