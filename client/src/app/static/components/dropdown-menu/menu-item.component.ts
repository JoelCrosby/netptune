import { Component, computed, input } from '@angular/core';
import { cva } from 'class-variance-authority';
import { cn } from '../button/button.variants';

// `warn` marks a destructive action such as delete or remove.
export type MenuItemColor = 'default' | 'warn';

const menuItemVariants = cva(
  'flex w-full items-center gap-3 px-3 py-2 text-sm text-left cursor-pointer select-none rounded-sm transition-colors hover:bg-neutral-100 dark:hover:bg-neutral-800 focus-visible:outline-none focus-visible:bg-neutral-100 dark:focus-visible:bg-neutral-800 disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      color: {
        default: '',
        warn: 'text-warn',
      },
    },
    defaultVariants: {
      color: 'default',
    },
  }
);

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-menu-item]',
  template: '<ng-content />',
  host: { type: 'button', '[class]': 'hostClass()' },
})
export class MenuItemComponent {
  readonly color = input<MenuItemColor>('default');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(menuItemVariants({ color: this.color() }), this.class());
  });
}
