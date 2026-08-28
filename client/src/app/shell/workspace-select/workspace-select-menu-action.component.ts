import { Component } from '@angular/core';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'a[app-workspace-menu-action], button[app-workspace-menu-action]',
  template: '<ng-content />',
  host: {
    class:
      'hover:bg-hover focus-visible:ring-primary flex w-full cursor-pointer items-center gap-2.5 rounded-[5px] px-2 py-1.75 text-left text-[13px] leading-6 tracking-[.225px] focus-visible:ring-2 focus-visible:outline-none',
  },
})
export class WorkspaceSelectMenuActionComponent {}
