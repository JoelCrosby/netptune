import { Component } from '@angular/core';

@Component({
  selector: 'app-card-subtitle',
  template: '<p><ng-content/></p>',
  host: {
    class: 'flex text-sm text-neutral-900 dark:text-white/70',
  },
})
export class CardSubtitleComponent {}
