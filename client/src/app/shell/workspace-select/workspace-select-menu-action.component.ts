import { Component } from '@angular/core';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'a[app-workspace-menu-action], button[app-workspace-menu-action]',
  template: '<ng-content />',
  host: {
    class:
      'text-foreground hover:bg-hover focus-visible:ring-primary my-[.2rem] block w-full cursor-pointer rounded-sm px-[.4rem] py-2 text-left text-sm leading-6 tracking-[.225px] focus-visible:ring-2 focus-visible:outline-none',
  },
})
export class WorkspaceSelectMenuActionComponent {}
