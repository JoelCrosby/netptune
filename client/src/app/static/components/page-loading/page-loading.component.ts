import { Component, input } from '@angular/core';
import { SpinnerComponent } from '../spinner/spinner.component';

@Component({
  selector: 'app-page-loading',
  imports: [SpinnerComponent],
  host: {
    class: 'block h-full',
    role: 'status',
    '[attr.aria-label]': 'label()',
  },
  template: `<div class="flex h-full flex-col items-center justify-center">
    <app-spinner [diameter]="diameter()" />
    <span class="sr-only">{{ label() }}</span>
  </div>`,
})
export class PageLoadingComponent {
  readonly diameter = input('32px');
  readonly label = input('Loading');
}
