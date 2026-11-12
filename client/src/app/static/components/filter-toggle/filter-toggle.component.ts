import { Component, computed, input } from '@angular/core';
import { cn } from '@static/components/button/button.variants';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-filter-toggle]',
  template: '<ng-content />',
  host: {
    type: 'button',
    '[class]': 'className()',
    '[attr.aria-pressed]': 'pressed()',
  },
})
export class FilterToggleComponent {
  readonly pressed = input(false);
  readonly class = input('');

  readonly className = computed(() => {
    return cn(
      'border-border hover:bg-foreground/5 focus-visible:ring-primary flex h-9 cursor-pointer items-center gap-2 rounded-md border px-3 text-sm transition-colors outline-none focus-visible:ring-2',
      this.pressed() &&
        'border-amber-400 bg-amber-400/10 text-amber-700 dark:text-amber-300',
      this.class()
    );
  });
}
