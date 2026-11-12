import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

@Component({
  selector: 'app-dialog-columns',
  host: { class: 'flex min-h-0 flex-1 flex-col' },
  template: `
    <div [class]="mainClass()">
      <ng-content />
    </div>

    <ng-content select="[dialogColumnsFooter]" />
  `,
})
export class DialogColumnsComponent {
  // Spacing for the main column; the default is the task dialogs' reading width.
  readonly bodyClass = input('flex flex-col gap-[18px] px-7 pt-6 pb-5');

  protected readonly mainClass = computed(() => {
    return cn('custom-scroll min-h-0 flex-1 overflow-y-auto', this.bodyClass());
  });
}
