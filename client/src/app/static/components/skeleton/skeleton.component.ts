import { Component, HostBinding, input } from '@angular/core';
import { cn } from '../button/button.variants';

@Component({
  selector: 'app-skeleton',
  template: '',
  host: {
    'aria-hidden': 'true',
  },
})
export class SkeletonComponent {
  readonly class = input('');

  @HostBinding('class') get className(): string {
    return cn(
      'bg-foreground/10 block animate-pulse rounded motion-reduce:animate-none',
      this.class()
    );
  }
}
