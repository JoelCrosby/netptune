import { Component } from '@angular/core';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'p[app-supporting-text]',
  host: {
    class:
      'text-foreground/55 text-[13px] leading-relaxed [&_strong]:text-foreground [&_strong]:font-bold',
  },
  template: '<ng-content />',
})
export class SupportingTextComponent {}
