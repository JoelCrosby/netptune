import { Component, inject, input, signal } from '@angular/core';
import {
  LucideChevronRight,
  LucideDynamicIcon,
  LucideIconInput,
} from '@lucide/angular';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { ShellService } from './shell.service';

export interface ShellMenuLink {
  label: string;
  value: string[];
  icon?: LucideIconInput;
  children?: ShellMenuLink[];
}

@Component({
  selector: 'app-shell-menu-link',
  imports: [
    RouterLinkActive,
    RouterLink,
    TooltipDirective,
    LucideDynamicIcon,
    LucideChevronRight,
  ],
  host: { class: 'block w-full' },
  template: ` @if (link(); as link) {
    <div
      class="hover:bg-side-bar-active/60 my-px flex w-full items-center overflow-hidden rounded text-sm font-medium text-white/70 transition-colors select-none"
      routerLinkActive="bg-side-bar-active text-white!">
      <a
        class="flex min-w-0 flex-1 cursor-pointer items-center gap-4 overflow-hidden py-2"
        [class.justify-center]="!shell.sideNavExpanded()"
        [class.pl-4]="shell.sideNavExpanded()"
        [routerLink]="link.value"
        [appTooltip]="shell.sideNavExpanded() ? '' : link.label"
        appTooltipPosition="right">
        @if (link.icon) {
          <svg
            [lucideIcon]="link.icon!"
            class="h-4 w-4 flex-none opacity-70"></svg>
        }

        <ng-content />

        @if (shell.sideNavExpanded()) {
          <span class="truncate transition-all transition-discrete">
            {{ link.label }}
          </span>
        }
      </a>

      @if (shell.sideNavExpanded() && link.children?.length) {
        <button
          type="button"
          class="mr-3 flex h-6 w-6 cursor-pointer items-center justify-center rounded-sm text-white/60 transition-colors hover:bg-white/10 hover:text-white"
          [attr.aria-expanded]="subMenuExpanded()"
          [attr.aria-label]="
            (subMenuExpanded() ? 'Collapse ' : 'Expand ') + link.label + ' menu'
          "
          (click)="toggleSubMenu($event)">
          <svg
            lucideChevronRight
            class="h-4 w-4 transition-transform"
            [class.rotate-90]="subMenuExpanded()"></svg>
        </button>
      }
    </div>

    @if (
      shell.sideNavExpanded() && link.children?.length && subMenuExpanded()
    ) {
      <div class="flex flex-col">
        @for (child of link.children; track child.value) {
          <app-shell-menu-link [link]="child" />
        }
      </div>
    }
  }`,
})
export class ShellMenuLinkComponent {
  shell = inject(ShellService);

  link = input.required<ShellMenuLink>();
  readonly subMenuExpanded = signal(false);

  toggleSubMenu(event: MouseEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.subMenuExpanded.update((expanded) => !expanded);
  }
}
