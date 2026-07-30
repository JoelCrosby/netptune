import { Component, input, output } from '@angular/core';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import {
  LucideCheck,
  LucideChevronLeft,
  LucideChevronRight,
  LucideRefreshCw,
} from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

type SprintOption = Pick<SprintViewModel, 'id' | 'name' | 'projectId'>;

@Component({
  selector: 'app-calendar-toolbar',
  imports: [
    DropdownButtonComponent,
    LucideCheck,
    LucideChevronLeft,
    LucideChevronRight,
    LucideRefreshCw,
    MenuItemComponent,
    StrokedButtonComponent,
  ],
  template: `
    <div
      class="border-border flex flex-wrap items-center gap-2 border-b p-3"
      role="toolbar"
      i18n-aria-label="Accessible name of the calendar toolbar"
      aria-label="Calendar controls">
      <app-dropdown-button
        #projectMenu
        [label]="projectLabel()"
        i18n-ariaLabel="Accessible label for the calendar project filter"
        ariaLabel="Filter calendar by project"
        buttonClass="min-w-44 max-w-64 justify-between">
        <button
          app-menu-item
          type="button"
          role="menuitemradio"
          [attr.aria-checked]="projectId() === undefined"
          (click)="projectChanged.emit(null); projectMenu.close()">
          <span class="flex h-4 w-4 items-center justify-center">
            @if (projectId() === undefined) {
              <svg lucideCheck class="h-4 w-4"></svg>
            }
          </span>
          <span i18n="Filter option including every project">All projects</span>
        </button>
        @for (project of projects(); track project.id) {
          <button
            app-menu-item
            type="button"
            role="menuitemradio"
            [attr.aria-checked]="projectId() === project.id"
            (click)="projectChanged.emit(project.id); projectMenu.close()">
            <span class="flex h-4 w-4 items-center justify-center">
              @if (projectId() === project.id) {
                <svg lucideCheck class="h-4 w-4"></svg>
              }
            </span>
            <span class="max-w-52 truncate">{{ project.name }}</span>
          </button>
        }
      </app-dropdown-button>

      <app-dropdown-button
        #sprintMenu
        [label]="sprintLabel()"
        i18n-ariaLabel="Accessible label for the calendar sprint filter"
        ariaLabel="Filter calendar by sprint"
        buttonClass="min-w-44 max-w-64 justify-between">
        <button
          app-menu-item
          type="button"
          role="menuitemradio"
          [attr.aria-checked]="sprintId() === undefined"
          (click)="sprintChanged.emit(null); sprintMenu.close()">
          <span class="flex h-4 w-4 items-center justify-center">
            @if (sprintId() === undefined) {
              <svg lucideCheck class="h-4 w-4"></svg>
            }
          </span>
          <span i18n="Filter option including every sprint">All sprints</span>
        </button>
        @for (sprint of filteredSprints(); track sprint.id) {
          <button
            app-menu-item
            type="button"
            role="menuitemradio"
            [attr.aria-checked]="sprintId() === sprint.id"
            (click)="sprintChanged.emit(sprint.id); sprintMenu.close()">
            <span class="flex h-4 w-4 items-center justify-center">
              @if (sprintId() === sprint.id) {
                <svg lucideCheck class="h-4 w-4"></svg>
              }
            </span>
            <span class="max-w-52 truncate">{{ sprint.name }}</span>
          </button>
        }
      </app-dropdown-button>

      <div class="ml-auto flex items-center gap-2">
        <button
          app-stroked-button
          color="neutral"
          type="button"
          i18n-aria-label="
            Accessible label for the button that moves back one month
          "
          aria-label="Previous month"
          i18n-title="Tooltip on the button that moves back one month"
          title="Previous month"
          (click)="monthNavigationRequested.emit(-1)">
          <svg lucideChevronLeft class="h-4 w-4"></svg>
        </button>
        <button
          app-stroked-button
          color="neutral"
          type="button"
          (click)="todayRequested.emit()">
          <span i18n="Button that jumps the calendar to today">Today</span>
        </button>
        <span
          class="text-primary min-w-32 text-center text-sm font-semibold"
          aria-live="polite">
          {{ monthLabel() }}
        </span>
        <button
          app-stroked-button
          color="neutral"
          type="button"
          i18n-aria-label="
            Accessible label for the button that moves forward one month
          "
          aria-label="Next month"
          i18n-title="Tooltip on the button that moves forward one month"
          title="Next month"
          (click)="monthNavigationRequested.emit(1)">
          <svg lucideChevronRight class="h-4 w-4"></svg>
        </button>
        <button
          app-stroked-button
          color="neutral"
          type="button"
          i18n-aria-label="
            Accessible label for the button that reloads the calendar
          "
          aria-label="Refresh calendar"
          i18n-title="Tooltip on the button that reloads the calendar"
          title="Refresh calendar"
          (click)="refreshRequested.emit()">
          <svg lucideRefreshCw class="h-4 w-4"></svg>
        </button>
      </div>
    </div>
  `,
  styles: ``,
})
export class CalendarToolbarComponent {
  readonly monthLabel = input.required<string>();
  readonly projectId = input<number>();
  readonly projects = input<ProjectViewModel[]>([]);
  readonly sprintId = input<number>();
  readonly sprints = input<SprintOption[]>([]);

  readonly projectChanged = output<number | null>();
  readonly sprintChanged = output<number | null>();
  readonly monthNavigationRequested = output<-1 | 1>();
  readonly todayRequested = output();
  readonly refreshRequested = output();

  protected projectLabel(): string {
    return (
      this.projects().find((project) => project.id === this.projectId())
        ?.name ?? 'All projects'
    );
  }

  protected filteredSprints(): SprintOption[] {
    const projectId = this.projectId();
    return this.sprints().filter(
      (sprint) => !projectId || sprint.projectId === projectId
    );
  }

  protected sprintLabel(): string {
    return (
      this.sprints().find((sprint) => sprint.id === this.sprintId())?.name ??
      'All sprints'
    );
  }
}
