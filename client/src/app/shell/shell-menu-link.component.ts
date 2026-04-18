import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
} from '@angular/core';
import { LucideDynamicIcon, LucideIconInput } from '@lucide/angular';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { ShellService } from './shell.service';

interface ShellMenuLink {
  label: string;
  value: string[];
  icon?: LucideIconInput;
}

@Component({
  selector: 'app-shell-menu-link',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLinkActive, RouterLink, TooltipDirective, LucideDynamicIcon],
  host: { class: 'block w-full' },
  template: ` @if (link(); as link) {
    <a
      class="hover:bg-side-bar-active/60 transition:background-color flex w-full cursor-pointer items-center justify-center gap-4 overflow-hidden py-4 text-sm font-medium text-white/70 select-none"
      [routerLink]="link.value"
      [class.px-6]="shell.sideNavExpanded()"
      [class.justify-start]="shell.sideNavExpanded()"
      [appTooltip]="shell.sideNavExpanded() ? '' : link.label"
      appTooltipPosition="right"
      routerLinkActive="bg-side-bar-active text-white!">
      @if (link.icon) {
        <svg [lucideIcon]="link.icon!" class="h-5 w-5"></svg>
      }

      <ng-content />

      @if (shell.sideNavExpanded()) {
        <span class="transition-all transition-discrete">
          {{ link.label }}
        </span>
      }
    </a>
  }`,
})
export class ShellMenuLinkComponent {
  link = input.required<ShellMenuLink>();
  shell = inject(ShellService);
}
