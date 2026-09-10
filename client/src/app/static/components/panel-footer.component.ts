import { Component, computed, input } from '@angular/core';
import { cn } from './button/button.variants';

@Component({
  selector: 'app-panel-footer',
  imports: [],
  host: {
    '[class]': 'hostClass()',
  },
  template: ` <ng-content /> `,
  styles: ``,
})
export class PanelFooterComponent {
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn('border-border block border-t px-6 py-4', this.class());
  });
}
