import { Component, input } from '@angular/core';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-command-palette-item]',
  template: '<ng-content />',
  host: {
    type: 'button',
    class:
      'aria-selected:bg-accent/10 aria-selected:text-accent-foreground relative flex w-full cursor-default items-center gap-2 rounded-sm px-2 py-2 outline-none select-none',
    '[attr.aria-selected]': "selected() ? 'true' : null",
  },
})
export class CommandPaletteItemComponent {
  readonly selected = input(false);
}
