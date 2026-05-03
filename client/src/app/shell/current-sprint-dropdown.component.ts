import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
} from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { netptunePermissions } from '@core/auth/permissions';
import {
  loadCurrentSprints,
  setSprintTaskFilter,
} from '@core/store/sprints/sprints.actions';
import {
  selectCurrentSprint,
  selectCurrentSprints,
  selectCurrentSprintsLoaded,
  selectSelectedSprintFilter,
  selectSelectedSprintFilterId,
} from '@core/store/sprints/sprints.selectors';
import {
  LucideCalendarDays,
  LucideCheck,
  LucideChevronDown,
  LucideFilterX,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { ButtonLinkComponent } from '@static/components/button/button-link.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-current-sprint-dropdown',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    ButtonLinkComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    LucideCalendarDays,
    LucideCheck,
    LucideChevronDown,
    LucideFilterX,
  ],
  template: `
    @if (canReadSprints() && currentSprintsLoaded()) {
      @if (currentSprints().length > 0) {
        <button
          #sprintTrigger
          type="button"
          class="border-border text-foreground hover:bg-foreground/5 focus-visible:ring-primary inline-flex h-10 max-w-72 cursor-pointer items-center gap-2 rounded border bg-transparent px-3 text-sm font-medium transition-colors focus-visible:ring-2 focus-visible:outline-none"
          aria-haspopup="menu"
          [attr.aria-label]="triggerLabel()"
          (click)="sprintMenu.toggle(sprintTrigger)">
          <svg lucideCalendarDays class="h-4 w-4 shrink-0"></svg>
          <span class="truncate">{{ triggerLabel() }}</span>
          <svg lucideChevronDown class="h-4 w-4 shrink-0 opacity-70"></svg>
        </button>

        <app-dropdown-menu #sprintMenu xPosition="before">
          <div
            class="text-muted-foreground px-3 py-2 text-xs font-semibold uppercase">
            Current sprint
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
                <span class="text-muted-foreground max-w-64 truncate text-xs">
                  {{ sprint.projectName }}
                </span>
              </span>
            </button>
          }

          @if (selectedSprintFilterId()) {
            <div class="border-border/50 my-1 border-t"></div>

            <button
              app-menu-item
              type="button"
              (click)="onSprintFilterRemoved(sprintMenu)">
              <svg lucideFilterX class="h-4 w-4 shrink-0"></svg>
              Remove sprint filter
            </button>
          }

          <div class="border-border/50 my-1 border-t"></div>

          <button
            app-menu-item
            type="button"
            (click)="onSprintsSelected(sprintMenu)">
            View all sprints
          </button>
        </app-dropdown-menu>
      } @else {
        <a app-button-link variant="filled" [routerLink]="['./sprints']">
          Start Sprint
        </a>
      }
    }
  `,
})
export class CurrentSprintDropdownComponent {
  private readonly store = inject(Store);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly canReadSprints = this.store.selectSignal(
    selectHasPermission(netptunePermissions.sprints.read)
  );
  readonly currentSprints = this.store.selectSignal(selectCurrentSprints);
  readonly currentSprint = this.store.selectSignal(selectCurrentSprint);
  readonly currentSprintsLoaded = this.store.selectSignal(
    selectCurrentSprintsLoaded
  );
  readonly selectedSprintFilterId = this.store.selectSignal(
    selectSelectedSprintFilterId
  );
  readonly selectedSprintFilter = this.store.selectSignal(
    selectSelectedSprintFilter
  );

  readonly triggerLabel = computed(() => {
    const selectedSprint = this.selectedSprintFilter();

    if (selectedSprint) {
      return selectedSprint.name;
    }

    const sprints = this.currentSprints();

    if (sprints.length === 1) {
      return this.currentSprint()?.name ?? 'Current sprint';
    }

    return `${sprints.length} active sprints`;
  });

  constructor() {
    effect(() => {
      if (this.canReadSprints()) {
        this.store.dispatch(loadCurrentSprints());
      }
    });
  }

  onSprintSelected(sprintId: number, menu: DropdownMenuComponent) {
    menu.close();
    this.store.dispatch(setSprintTaskFilter({ sprintId }));
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
