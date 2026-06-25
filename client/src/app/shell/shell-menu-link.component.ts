import { Component, computed, inject, input, signal } from '@angular/core';
import {
  LucideChevronRight,
  LucideDynamicIcon,
  LucideIconInput,
  LucideLayoutGrid,
} from '@lucide/angular';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { ShellService } from './shell.service';

export interface ShellMenuLink {
  label: string;
  value?: string[];
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
      routerLinkActive="bg-side-bar-active text-white!"
      [routerLinkActiveOptions]="{ exact: true }">
      @if (expandable()) {
        <button
          type="button"
          class="flex min-w-0 flex-1 cursor-pointer items-center gap-4 overflow-hidden py-2 pl-4 text-left"
          [attr.aria-expanded]="subMenuExpanded()"
          [attr.aria-label]="
            (subMenuExpanded() ? 'Collapse ' : 'Expand ') + link.label + ' menu'
          "
          (click)="toggleSubMenu()">
          @if (link.icon) {
            <svg
              [lucideIcon]="link.icon!"
              class="h-4 w-4 flex-none opacity-70"></svg>
          }

          <ng-content />

          <span class="flex-1 truncate transition-all transition-discrete">
            {{ link.label }}
          </span>

          <svg
            lucideChevronRight
            class="mr-3 h-4 w-4 flex-none text-white/60 transition-transform"
            [class.rotate-90]="subMenuExpanded()"></svg>
        </button>
      } @else {
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
      }
    </div>

    @if (expandable()) {
      <div
        class="grid transition-[grid-template-rows] duration-200 ease-out"
        [style.grid-template-rows]="subMenuExpanded() ? '1fr' : '0fr'">
        <div class="overflow-hidden">
          <div class="mb-2 ml-6 flex flex-col border-l border-white/10 pl-2">
            @for (child of childLinks(); track child.value) {
              <app-shell-menu-link [link]="child" />
            }
          </div>
        </div>
      </div>
    }
  }`,
})
export class ShellMenuLinkComponent {
  shell = inject(ShellService);

  link = input.required<ShellMenuLink>();
  lucideLayoutGrid = LucideLayoutGrid;
  readonly subMenuExpanded = signal(false);

  /** The parent link plus an "Overview" entry pointing at the parent route. */
  readonly childLinks = computed<ShellMenuLink[]>(() => {
    const link = this.link();
    const children = link.children;

    if (!children?.length) {
      return [];
    }

    const overview = link.value
      ? [
          {
            label: 'Overview',
            value: link.value,
            icon: this.lucideLayoutGrid,
          },
        ]
      : [];

    return [...overview, ...children];
  });

  readonly expandable = computed(
    () => this.shell.sideNavExpanded() && this.childLinks().length > 0
  );

  toggleSubMenu() {
    this.subMenuExpanded.update((expanded) => !expanded);
  }
}
