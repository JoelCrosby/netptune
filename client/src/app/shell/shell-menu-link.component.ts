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
    @if (shell.sideNavExpanded() && link.children?.length) {
      <div
        class="border-side-bar-border hover:bg-side-bar-active/60 flex w-full items-center overflow-hidden text-sm font-medium text-white/70 transition-colors select-none"
        [class.border-t]="subMenuExpanded()"
        routerLinkActive="bg-side-bar-active text-white!">
        <a
          class="flex min-w-0 flex-1 cursor-pointer items-center gap-4 overflow-hidden py-4 pl-6"
          [routerLink]="link.value">
          @if (link.icon) {
            <svg [lucideIcon]="link.icon!" class="h-5 w-5 flex-none"></svg>
          }

          <ng-content />

          <span class="truncate transition-all transition-discrete">
            {{ link.label }}
          </span>
        </a>

        <button
          type="button"
          class="mr-3 flex h-8 w-8 cursor-pointer items-center justify-center rounded-sm text-white/60 transition-colors hover:bg-white/10 hover:text-white"
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
      </div>
    } @else {
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
    }

    @if (
      shell.sideNavExpanded() && link.children?.length && subMenuExpanded()
    ) {
      <div class="border-side-bar-border flex flex-col border-b">
        @for (child of link.children; track child.value) {
          <a
            class="hover:bg-side-bar-active/60 transition:background-color flex w-full cursor-pointer items-center justify-start gap-4 overflow-hidden px-6 py-4 text-sm font-medium text-white/70 select-none"
            [routerLink]="child.value"
            appTooltipPosition="right"
            routerLinkActive="bg-side-bar-active text-white!">
            @if (child.icon) {
              <svg [lucideIcon]="child.icon" class="h-5 w-5"></svg>
            }

            <ng-content />

            <span class="transition-all transition-discrete">
              {{ child.label }}
            </span>
          </a>
        }
      </div>
    }
  }`,
})
export class ShellMenuLinkComponent {
  link = input.required<ShellMenuLink>();
  shell = inject(ShellService);
  readonly subMenuExpanded = signal(false);

  toggleSubMenu(event: MouseEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.subMenuExpanded.update((expanded) => !expanded);
  }
}
