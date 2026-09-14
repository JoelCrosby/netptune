import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

export type DialogSectionDivider = 'none' | 'top' | 'bottom';

const dividerClasses: Record<DialogSectionDivider, string> = {
  none: '',
  top: 'border-t',
  bottom: 'border-b',
};

// A band inside a dialog column, ruled off from its neighbour with the dialog's
// own softer line rather than the panel border. Padding comes from `class`.
@Component({
  selector: 'app-dialog-section, [app-dialog-section]',
  host: { '[class]': 'hostClass()' },
  template: '<ng-content />',
})
export class DialogSectionComponent {
  readonly divider = input<DialogSectionDivider>('none');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      'border-foreground/8 block',
      dividerClasses[this.divider()],
      this.class()
    );
  });
}
