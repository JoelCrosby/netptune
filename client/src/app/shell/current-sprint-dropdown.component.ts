import { Component, computed, inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { Router, ActivatedRoute } from '@angular/router';
import { CurrentRouteService } from '@core/router/current-route.service';
import { PERMISSIONS } from '@core/auth/permissions';
import { SprintFilterService } from '@core/services/sprint-filter.service';
import {
  currentSprintsResource,
  sprintResource,
} from '@core/resources/sprint.resource';
import {
  LucideCalendarDays,
  LucideCheck,
  LucideChevronDown,
  LucideExternalLink,
  LucideFilterX,
} from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { SprintDaysBadgeComponent } from '@static/components/sprint-days-badge.component';

@Component({
  selector: 'app-current-sprint-dropdown',
  imports: [
    DropdownMenuComponent,
    MenuItemComponent,
    LucideCalendarDays,
    LucideCheck,
    LucideChevronDown,
    LucideExternalLink,
    LucideFilterX,
    SprintDaysBadgeComponent,
  ],
  template: `
    @if (
      isSprintFilterableRoute() &&
      canReadSprints() &&
      currentSprints().length > 0
    ) {
      <button
        #sprintTrigger
        type="button"
        class="border-border text-foreground hover:bg-foreground/5 focus-visible:ring-primary inline-flex h-10 max-w-96 cursor-pointer items-center gap-2 rounded border bg-transparent px-3 text-sm font-medium transition-colors focus-visible:ring-2 focus-visible:outline-none"
        aria-haspopup="menu"
        [attr.aria-label]="triggerLabel()"
        (click)="sprintMenu.toggle(sprintTrigger)">
        <svg lucideCalendarDays class="h-4 w-4 shrink-0"></svg>
        <span class="truncate">{{ triggerLabel() }}</span>

        @if (selectedSprintFilter(); as selectedSprint) {
          <app-sprint-days-badge
            [status]="selectedSprint.status"
            [endDate]="selectedSprint.endDate" />
        }

        <svg lucideChevronDown class="h-4 w-4 shrink-0 opacity-70"></svg>
      </button>

      <app-dropdown-menu #sprintMenu xPosition="before">
        <div class="text-muted px-3 py-2 text-xs font-semibold uppercase">
          <span i18n="Heading above the list of currently active sprints">
            Current sprint
          </span>
        </div>

        @for (sprint of currentSprints(); track sprint.id) {
          <button
            app-menu-item
            type="button"
            class="min-w-72"
            (click)="onSprintSelected(sprint.id, sprintMenu)">
            <svg
              lucideCheck
              class="h-4 w-4 shrink-0"
              [class.opacity-100]="selectedSprintFilterId() === sprint.id"
              [class.opacity-0]="selectedSprintFilterId() !== sprint.id"
              aria-hidden="true"></svg>
            <span class="flex min-w-0 flex-col items-start">
              <span class="max-w-64 truncate font-medium">
                {{ sprint.name }}
              </span>
              <span class="text-muted max-w-64 truncate text-xs">
                {{ sprint.projectName }}
              </span>
            </span>
          </button>
        }

        @if (selectedSprintFilter(); as selectedSprint) {
          <div class="border-border/50 my-1 border-t"></div>

          <button
            app-menu-item
            type="button"
            (click)="onSprintOpened(selectedSprint.id, sprintMenu)">
            <svg lucideExternalLink class="h-4 w-4 shrink-0"></svg>
            <span
              class="max-w-64 truncate"
              i18n="
                Menu item that opens the sprint currently being filtered on.
                SPRINT_NAME is the sprint's name
              ">
              Open
              {{
                selectedSprint.name // i18n(ph="SPRINT_NAME")
              }}
            </span>
          </button>

          <button
            app-menu-item
            type="button"
            (click)="onSprintFilterRemoved(sprintMenu)">
            <svg lucideFilterX class="h-4 w-4 shrink-0"></svg>
            <span i18n="Menu item that clears the active sprint filter">
              Remove sprint filter
            </span>
          </button>
        }

        <div class="border-border/50 my-1 border-t"></div>

        <button
          app-menu-item
          type="button"
          (click)="onSprintsSelected(sprintMenu)">
          <span i18n="Menu item that navigates to the full sprint list">
            View all sprints
          </span>
        </button>
      </app-dropdown-menu>
    }
  `,
})
export class CurrentSprintDropdownComponent {
  private readonly sprintFilter = inject(SprintFilterService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  isSprintFilterableRoute = inject(CurrentRouteService).isSprintFilterableRoute;
  canReadSprints = hasPermission(PERMISSIONS.sprints.read);
  private readonly currentSprintsRef = currentSprintsResource();
  private readonly allSprintsRef = sprintResource([]);

  currentSprints = this.currentSprintsRef.value;
  selectedSprintFilterId = this.sprintFilter.sprintId;

  selectedSprintFilter = computed(() => {
    const sprintId = this.selectedSprintFilterId();

    if (sprintId === undefined) return undefined;

    return (
      this.currentSprints().find((sprint) => sprint.id === sprintId) ??
      this.allSprintsRef.value().find((sprint) => sprint.id === sprintId)
    );
  });

  triggerLabel = computed(() => {
    const selectedSprint = this.selectedSprintFilter();

    if (selectedSprint) {
      return selectedSprint.name;
    }

    const count = this.currentSprints().length;
    const isSingle = count === 1;

    return isSingle
      ? $localize`:Sprint dropdown label when exactly one sprint is active:1 active sprint`
      : $localize`:Sprint dropdown label showing how many sprints are active. COUNT is never 1:${count}:COUNT: active sprints`;
  });

  onSprintSelected(sprintId: number, menu: DropdownMenuComponent) {
    menu.close();
    this.sprintFilter.set(sprintId);
  }

  onSprintOpened(sprintId: number, menu: DropdownMenuComponent) {
    menu.close();
    void this.router.navigate(['./sprints', sprintId], {
      relativeTo: this.route,
    });
  }

  onSprintFilterRemoved(menu: DropdownMenuComponent) {
    menu.close();
    this.sprintFilter.clear();
  }

  onSprintsSelected(menu: DropdownMenuComponent) {
    menu.close();
    void this.router.navigate(['./sprints'], { relativeTo: this.route });
  }
}
