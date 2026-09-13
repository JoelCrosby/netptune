import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

@Component({
  selector: 'app-menu-separator',
  host: {
    role: 'separator',
    '[class]': 'hostClass()',
  },
  template: '',
})
export class MenuSeparatorComponent {
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn('border-border/50 my-1 block border-t', this.class());
  });
}
