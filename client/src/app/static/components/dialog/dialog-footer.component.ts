import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

// The fixed action bar along the bottom of a full-height dialog.
@Component({
  selector: 'app-dialog-footer, [app-dialog-footer]',
  host: { '[class]': 'hostClass()' },
  template: '<ng-content />',
})
export class DialogFooterComponent {
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      'border-foreground/8 bg-foreground/2 flex shrink-0 items-center justify-end gap-2 border-t px-5 py-3',
      this.class()
    );
  });
}
