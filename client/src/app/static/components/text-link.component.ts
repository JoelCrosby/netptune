import { Component, computed, input } from '@angular/core';
import { cva } from 'class-variance-authority';
import { cn } from './button/button.variants';

export type TextLinkColor = 'primary' | 'muted' | 'inherit';
export type TextLinkSize = 'default' | 'small';

const textLinkVariants = cva(
  'cursor-pointer font-semibold underline-offset-2 transition-colors hover:underline focus-visible:rounded-xs focus-visible:ring-2 focus-visible:outline-none',
  {
    variants: {
      color: {
        primary: 'text-primary focus-visible:ring-primary',
        muted: 'text-foreground/60 hover:text-foreground',
        inherit: 'text-inherit',
      },
      size: {
        default: 'text-[inherit]',
        small: 'text-xs',
      },
    },
    defaultVariants: {
      color: 'primary',
      size: 'default',
    },
  }
);

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'a[app-text-link], button[app-text-link]',
  template: '<ng-content />',
  host: { '[class]': 'hostClass()' },
})
export class TextLinkComponent {
  readonly color = input<TextLinkColor>('primary');
  readonly size = input<TextLinkSize>('default');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      textLinkVariants({ color: this.color(), size: this.size() }),
      this.class()
    );
  });
}
