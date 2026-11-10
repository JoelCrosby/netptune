import { Component } from '@angular/core';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'ul[app-bullet-list]',
  host: { class: 'list-disc pl-5' },
  template: '<ng-content />',
})
export class BulletListComponent {}
