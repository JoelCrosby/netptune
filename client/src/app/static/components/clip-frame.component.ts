import { Component, computed, input } from '@angular/core';
import { cn } from './button/button.variants';

@Component({
  selector: 'app-clip-frame',
  template: '<ng-content />',
  host: { '[class]': 'hostClass()' },
})
export class ClipFrameComponent {
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn('block overflow-hidden rounded-lg leading-none', this.class());
  });
}
