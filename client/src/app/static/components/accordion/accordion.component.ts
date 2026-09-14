import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

// Groups `app-accordion-row`s under a single rule. Each row draws the line below
// itself, so mark the final row `last`.
@Component({
  selector: 'app-accordion, [app-accordion]',
  host: { '[class]': 'hostClass()' },
  template: '<ng-content />',
})
export class AccordionComponent {
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn('border-foreground/8 flex flex-col border-t', this.class());
  });
}
