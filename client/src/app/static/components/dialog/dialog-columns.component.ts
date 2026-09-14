import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

// The body of a two-column dialog: a scrolling main column, an optional band pinned
// under it (`dialogColumnsFooter`), and a projected `app-dialog-rail`. Below 1200px
// the columns stack.
@Component({
  selector: 'app-dialog-columns',
  host: { class: 'flex min-h-0 flex-1 flex-row max-[1200px]:flex-col' },
  template: `
    <div class="flex min-w-0 flex-1 flex-col">
      <div [class]="mainClass()">
        <ng-content />
      </div>

      <ng-content select="[dialogColumnsFooter]" />
    </div>

    <ng-content select="app-dialog-rail, [app-dialog-rail]" />
  `,
})
export class DialogColumnsComponent {
  // Spacing for the main column; the default is the task dialogs' reading width.
  readonly bodyClass = input('flex flex-col gap-[18px] px-7 pt-6 pb-5');

  protected readonly mainClass = computed(() => {
    return cn('custom-scroll min-h-0 flex-1 overflow-y-auto', this.bodyClass());
  });
}
