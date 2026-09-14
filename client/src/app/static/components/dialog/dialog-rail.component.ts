import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

// The tinted side column of a two-column dialog. Below 1200px the dialog stacks,
// so the rail drops underneath the main column and runs full width.
@Component({
  selector: 'app-dialog-rail, [app-dialog-rail]',
  host: { '[class]': 'hostClass()' },
  template: '<ng-content />',
})
export class DialogRailComponent {
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      'border-foreground/8 bg-foreground/2 flex w-[340px] shrink-0 flex-col border-l max-[1200px]:w-full max-[1200px]:border-t max-[1200px]:border-l-0',
      this.class()
    );
  });
}
