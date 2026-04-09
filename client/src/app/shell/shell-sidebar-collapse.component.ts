import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';
import { ShellService } from './shell.service';

@Component({
  selector: 'app-shell-sidebar-collapse',
  template: `
    <div
      class="hover:bg-side-bar-active/60 border-side-bar-border transition:background-color flex w-full cursor-pointer items-center justify-center gap-4 overflow-hidden rounded-sm border-t py-4 text-sm font-medium text-white/70 select-none"
      [class.px-8]="shell.sideNavExpanded()"
      [class.justify-start]="shell.sideNavExpanded()"
      (click)="shell.toggleSidebar()"
      [matTooltip]="shell.sideNavExpanded() ? '' : 'Expand'"
      matTooltipPosition="right"
      role="button">
      <mat-icon mat-list-icon class="material-icons-round">
        {{
          shell.sideNavExpanded()
            ? 'keyboard_arrow_left'
            : 'keyboard_arrow_right'
        }}
      </mat-icon>
      @if (shell.sideNavExpanded()) {
        <p>Collapse</p>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatTooltip, MatIcon],
})
export class ShellSidebarCollapseComponent {
  shell = inject(ShellService);
}
