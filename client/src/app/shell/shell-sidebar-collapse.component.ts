import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideChevronLeft, LucideChevronRight } from '@lucide/angular';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { ShellService } from './shell.service';

@Component({
  selector: 'app-shell-sidebar-collapse',
  template: `
    <div
      class="hover:bg-side-bar-active/60 border-side-bar-border transition:background-color flex w-full cursor-pointer items-center justify-center gap-4 overflow-hidden border-t py-4 text-sm font-medium text-white/70 select-none"
      [class.px-8]="shell.sideNavExpanded()"
      [class.justify-start]="shell.sideNavExpanded()"
      (click)="shell.toggleSidebar()"
      [appTooltip]="shell.sideNavExpanded() ? '' : 'Expand'"
      appTooltipPosition="right"
      role="button">
      @if (shell.sideNavExpanded()) {
        <svg lucideChevronLeft class="h-5 w-5"></svg>
      } @else {
        <svg lucideChevronRight class="h-5 w-5"></svg>
      }
      @if (shell.sideNavExpanded()) {
        <p>Collapse</p>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TooltipDirective, LucideChevronLeft, LucideChevronRight],
})
export class ShellSidebarCollapseComponent {
  shell = inject(ShellService);
}
