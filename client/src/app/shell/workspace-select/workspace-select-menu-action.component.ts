import { Component } from '@angular/core';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'a[app-workspace-menu-action], button[app-workspace-menu-action]',
  template: '<ng-content />',
  host: {
    class:
      'text-foreground hover:bg-hover focus-visible:ring-primary flex w-full cursor-pointer items-center gap-2.5 rounded-sm px-2 py-2 text-left text-sm leading-6 tracking-[.225px] focus-visible:ring-2 focus-visible:outline-none',
  },
})
export class WorkspaceSelectMenuActionComponent {}
