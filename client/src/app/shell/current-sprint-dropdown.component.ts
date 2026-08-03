import { Component, computed, effect, inject } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { selectIsSprintFilterableRoute } from '@core/core.route.selectors';
import { netptunePermissions } from '@core/auth/permissions';
import {
  loadCurrentSprints,
  setSprintTaskFilter,
} from '@core/store/sprints/sprints.actions';
import {
  selectCurrentSprints,
  selectCurrentSprintsLoaded,
  selectSelectedSprintFilter,
  selectSelectedSprintFilterId,
} from '@core/store/sprints/sprints.selectors';
import {
  LucideCalendarDays,
  LucideCalendarFold,
  LucideCheck,
  LucideChevronDown,
  LucideExternalLink,
  LucideFilterX,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { ButtonLinkComponent } from '@static/components/button/button-link.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { SprintDaysBadgeComponent } from '@static/components/sprint-days-badge.component';

@Component({
  selector: 'app-current-sprint-dropdown',
  imports: [
    RouterLink,
    ButtonLinkComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    LucideCalendarDays,
    LucideCheck,
    LucideChevronDown,
    LucideExternalLink,
    LucideFilterX,
    LucideCalendarFold,
    SprintDaysBadgeComponent,
  ],
  template: `
    @if (
      isSprintFilterableRoute() && canReadSprints() && currentSprintsLoaded()
    ) {
      @if (currentSprints().length > 0) {
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
      } @else {
        <a
          app-button-link
          variant="filled"
          color="contrast"
          class="tems-center mr-2 flex h-4 justify-center"
          [routerLink]="['./sprints']">
          <svg lucideCalendarFold class="w-4"></svg>
          <span
            i18n="
              Navbar link shown when no sprint is active, opens the sprint list
            ">
            Start Sprint
          </span>
        </a>
      }
    }
  `,
})
export class CurrentSprintDropdownComponent {
  private readonly store = inject(Store);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  isSprintFilterableRoute = this.store.selectSignal(
    selectIsSprintFilterableRoute
  );
  canReadSprints = this.store.selectSignal(
    selectHasPermission(netptunePermissions.sprints.read)
  );
  currentSprints = this.store.selectSignal(selectCurrentSprints);
  currentSprintsLoaded = this.store.selectSignal(selectCurrentSprintsLoaded);
  selectedSprintFilterId = this.store.selectSignal(
    selectSelectedSprintFilterId
  );
  selectedSprintFilter = this.store.selectSignal(selectSelectedSprintFilter);

  triggerLabel = computed(() => {
    const selectedSprint = this.selectedSprintFilter();

    if (selectedSprint) {
      return selectedSprint.name;
    }

    const count = this.currentSprints().length;
    const isSingle = count === 1;

    // $localize does not evaluate ICU, so this is a ternary rather than a plural
    // expression. fr/de/es share English's one/other split; a locale with more
    // plural categories (ru, pl, ar) means moving this into a template ICU.
    return isSingle
      ? $localize`:Sprint dropdown label when exactly one sprint is active:1 active sprint`
      : $localize`:Sprint dropdown label showing how many sprints are active. COUNT is never 1:${count}:COUNT: active sprints`;
  });

  constructor() {
    effect(() => {
      if (this.canReadSprints()) {
        this.store.dispatch(loadCurrentSprints.init());
      }
    });
  }

  onSprintSelected(sprintId: number, menu: DropdownMenuComponent) {
    menu.close();
    this.store.dispatch(setSprintTaskFilter({ sprintId }));
  }

  onSprintOpened(sprintId: number, menu: DropdownMenuComponent) {
    menu.close();
    void this.router.navigate(['./sprints', sprintId], {
      relativeTo: this.route,
    });
  }

  onSprintFilterRemoved(menu: DropdownMenuComponent) {
    menu.close();
    this.store.dispatch(setSprintTaskFilter({ sprintId: undefined }));
  }

  onSprintsSelected(menu: DropdownMenuComponent) {
    menu.close();
    void this.router.navigate(['./sprints'], { relativeTo: this.route });
  }
}
