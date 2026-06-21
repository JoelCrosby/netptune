import { Component, inject } from '@angular/core';
import { LucidePanelLeftClose, LucidePanelLeftOpen } from '@lucide/angular';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { ShellService } from './shell.service';

@Component({
  selector: 'app-shell-sidebar-collapse',
  host: {
    class: 'block w-full border-side-bar-border border-t h-14 py-2 px-2',
  },
  template: `
    <div
      class="hover:bg-side-bar-active/60 transition:background-color flex w-full cursor-pointer items-center justify-center gap-4 overflow-hidden rounded px-4 py-2 text-sm font-medium text-white/70 select-none"
      [class.justify-start]="shell.sideNavExpanded()"
      (click)="shell.toggleSidebar()"
      [appTooltip]="shell.sideNavExpanded() ? '' : 'Expand'"
      appTooltipPosition="right"
      role="button">
      @if (shell.sideNavExpanded()) {
        <svg lucidePanelLeftClose class="h-5 w-5"></svg>
      } @else {
        <svg lucidePanelLeftOpen class="h-5 w-5"></svg>
      }
      @if (shell.sideNavExpanded()) {
        <p>Collapse</p>
      }
    </div>
  `,
  imports: [TooltipDirective, LucidePanelLeftClose, LucidePanelLeftOpen],
})
export class ShellSidebarCollapseComponent {
  shell = inject(ShellService);
}
