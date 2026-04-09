import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
} from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { ShellService } from './shell.service';

interface ShellMenuLink {
  label: string;
  value: string[];
  icon?: string;
}

@Component({
  selector: 'app-shell-menu-link',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLinkActive, RouterLink, TooltipDirective, MatIcon],
  host: { class: 'block w-full' },
  template: ` <a
    class="hover:bg-side-bar-active/60 transition:background-color flex w-full cursor-pointer items-center justify-center gap-4 overflow-hidden rounded-sm py-4 text-sm font-medium text-white/70 select-none"
    [routerLink]="link().value"
    [class.px-8]="shell.sideNavExpanded()"
    [class.justify-start]="shell.sideNavExpanded()"
    [appTooltip]="shell.sideNavExpanded() ? '' : link().label"
    appTooltipPosition="right"
    routerLinkActive="bg-side-bar-active text-white!">
    @if (link().icon) {
      <mat-icon class="material-icons-outline">
        {{ link().icon }}
      </mat-icon>
    }

    <ng-content />

    <span
      [class.block!]="shell.sideNavExpanded()"
      class="hidden transition-all transition-discrete"
      >{{ link().label }}</span
    >
  </a>`,
})
export class ShellMenuLinkComponent {
  link = input.required<ShellMenuLink>();
  shell = inject(ShellService);
}
