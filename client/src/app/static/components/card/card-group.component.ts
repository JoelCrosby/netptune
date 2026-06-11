import { Component } from '@angular/core';

@Component({
  selector: 'app-card-group',
  template: '<ng-content/>',
  host: {
    class: 'inline-flex flex-wrap gap-4 p-2 mb-2',
  },
})
export class CardGroupComponent {}
