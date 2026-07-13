import { Component, input } from '@angular/core';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-workspace-select-option]',
  template: '<ng-content />',
  host: {
    type: 'button',
    class:
      'hover:bg-hover focus-visible:ring-primary my-[.2rem] flex h-9.5 w-full cursor-pointer items-center rounded-sm px-2 text-left font-[inherit] text-sm focus-visible:ring-2 focus-visible:outline-none',
    '[class.bg-primary]': 'active()',
    '[attr.aria-current]': "active() ? 'true' : null",
  },
})
export class WorkspaceSelectOptionComponent {
  readonly active = input(false);
}
