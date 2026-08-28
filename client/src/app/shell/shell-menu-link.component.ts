import { Component, computed, inject, input, signal } from '@angular/core';
import {
  LucideChevronRight,
  LucideDynamicIcon,
  LucideIconInput,
  LucideLayoutGrid,
} from '@lucide/angular';
import {
  IsActiveMatchOptions,
  RouterLink,
  RouterLinkActive,
} from '@angular/router';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { ShellService } from './shell.service';

export interface ShellMenuLink {
  label: string;
  value?: string[];
  icon?: LucideIconInput;
  children?: ShellMenuLink[];
  overviewLabel?: string;
  overviewIcon?: LucideIconInput;
  count?: number;
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
  template: `
    @if (link(); as link) {
      <div
        class="outline-foreground focus-visible:ring-2:focus-visible ring-foreground hover:bg-side-bar-active/60 my-px flex w-full items-center rounded text-sm font-medium text-white/70 transition-colors select-none"
        routerLinkActive="bg-side-bar-active text-white!"
        [routerLinkActiveOptions]="activeOptions">
        @if (expandable()) {
          <button
            type="button"
            class="flex min-w-0 flex-1 cursor-pointer items-center gap-4 overflow-hidden py-2 pl-4 text-left"
            [attr.aria-expanded]="subMenuExpanded()"
            [attr.aria-label]="subMenuLabel()"
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

            @if (link.count) {
              <span
                class="bg-primary/18 text-primary inline-flex h-[18px] min-w-5 items-center justify-center rounded-full px-1.5 text-[11px] font-semibold tabular-nums">
                {{ link.count }}
              </span>
            }

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
              <span class="flex-1 truncate transition-all transition-discrete">
                {{ link.label }}
              </span>

              @if (link.count) {
                <span
                  class="bg-primary/18 text-primary mr-3 inline-flex h-[18px] min-w-5 items-center justify-center rounded-full px-1.5 text-[11px] font-semibold tabular-nums">
                  {{ link.count }}
                </span>
              }
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
    }
  `,
})
export class ShellMenuLinkComponent {
  shell = inject(ShellService);

  link = input.required<ShellMenuLink>();
  lucideLayoutGrid = LucideLayoutGrid;
  readonly subMenuExpanded = signal(false);

  protected readonly activeOptions: IsActiveMatchOptions = {
    paths: 'exact',
    queryParams: 'ignored',
    fragment: 'ignored',
    matrixParams: 'ignored',
  };

  readonly childLinks = computed<ShellMenuLink[]>(() => {
    const link = this.link();
    const children = link.children;

    if (!children?.length) {
      return [];
    }

    const overview = link.value
      ? [
          {
            label:
              link.overviewLabel ??
              $localize`:Sub-menu entry linking to the parent section's own page:Overview`,
            value: link.value,
            icon: link.overviewIcon ?? this.lucideLayoutGrid,
          },
        ]
      : [];

    return [...overview, ...children];
  });

  readonly expandable = computed(
    () => this.shell.sideNavExpanded() && this.childLinks().length > 0
  );

  readonly subMenuLabel = computed(() => {
    const section = this.link().label;

    return this.subMenuExpanded()
      ? $localize`:Accessible label for the control that collapses a sidebar sub-menu. SECTION is the section name, e.g. Sprints:Collapse ${section}:SECTION: menu`
      : $localize`:Accessible label for the control that expands a sidebar sub-menu. SECTION is the section name, e.g. Sprints:Expand ${section}:SECTION: menu`;
  });

  toggleSubMenu() {
    this.subMenuExpanded.update((expanded) => !expanded);
  }
}
