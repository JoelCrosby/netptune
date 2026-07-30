import { Component, input, output } from '@angular/core';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import {
  LucideCheck,
  LucideChevronLeft,
  LucideChevronRight,
} from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { DateDropdownButtonComponent } from '@static/components/dropdown-menu/date-dropdown-button.component';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { TimelineZoom } from '@static/components/timeline/timeline.models';

interface ZoomOption {
  label: string;
  value: TimelineZoom;
}

type SprintOption = Pick<SprintViewModel, 'id' | 'name' | 'projectId'>;

@Component({
  selector: 'app-roadmap-filters',
  imports: [
    DateDropdownButtonComponent,
    DropdownButtonComponent,
    CheckboxComponent,
    LucideCheck,
    LucideChevronLeft,
    LucideChevronRight,
    MenuItemComponent,
    StrokedButtonComponent,
  ],
  template: `
    <div
      class="border-border flex flex-wrap items-center gap-2 border-b p-3"
      i18n-aria-label="Accessible name of the roadmap filter bar"
      aria-label="Roadmap filters">
      <app-date-dropdown-button
        i18n-label="Label of the start-date filter"
        label="From"
        i18n-ariaLabel="Accessible label for the roadmap start date"
        ariaLabel="Roadmap start date"
        buttonClass="min-w-44 justify-between"
        [value]="from()"
        (valueChanged)="fromChanged.emit($event)" />

      <app-date-dropdown-button
        i18n-label="Label of the end-date filter"
        label="To"
        i18n-ariaLabel="Accessible label for the roadmap end date"
        ariaLabel="Roadmap end date"
        buttonClass="min-w-44 justify-between"
        [value]="to()"
        (valueChanged)="toChanged.emit($event)" />

      <app-dropdown-button
        #zoomMenu
        [label]="zoomLabel()"
        i18n-ariaLabel="Accessible label for the roadmap zoom control"
        ariaLabel="Roadmap zoom"
        buttonClass="min-w-32 justify-between">
        @for (option of zoomOptions; track option.value) {
          <button
            app-menu-item
            type="button"
            role="menuitemradio"
            [attr.aria-checked]="zoom() === option.value"
            (click)="zoomChanged.emit(option.value); zoomMenu.close()">
            <span class="flex h-4 w-4 items-center justify-center">
              @if (zoom() === option.value) {
                <svg lucideCheck class="h-4 w-4"></svg>
              }
            </span>
            <span>{{ option.label }}</span>
          </button>
        }
      </app-dropdown-button>

      <app-dropdown-button
        #sprintMenu
        [label]="sprintLabel()"
        i18n-ariaLabel="Accessible label for the roadmap sprint filter"
        ariaLabel="Filter roadmap by sprint"
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

      <app-dropdown-button
        #projectMenu
        [label]="projectLabel()"
        i18n-ariaLabel="Accessible label for the roadmap project filter"
        ariaLabel="Filter roadmap by project"
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

      <app-checkbox
        class="px-2 text-sm"
        [checked]="includeUnscheduled()"
        (changed)="includeUnscheduledChanged.emit($event)">
        <span i18n="Toggle that reveals tasks without dates">
          Show unscheduled
        </span>
      </app-checkbox>

      <div class="ml-auto flex items-center gap-2">
        <button
          app-stroked-button
          color="neutral"
          type="button"
          i18n-aria-label="
            Accessible label for the button that moves back one range
          "
          aria-label="Previous date range"
          i18n-title="Tooltip on the button that moves back one range"
          title="Previous date range"
          (click)="rangeNavigationRequested.emit(-1)">
          <svg lucideChevronLeft class="h-4 w-4"></svg>
        </button>
        <button
          app-stroked-button
          color="neutral"
          type="button"
          (click)="todayRequested.emit()">
          <span i18n="Button that jumps the roadmap to today">Today</span>
        </button>
        <button
          app-stroked-button
          color="neutral"
          type="button"
          i18n-aria-label="
            Accessible label for the button that moves forward one range
          "
          aria-label="Next date range"
          i18n-title="Tooltip on the button that moves forward one range"
          title="Next date range"
          (click)="rangeNavigationRequested.emit(1)">
          <svg lucideChevronRight class="h-4 w-4"></svg>
        </button>
        <button
          app-stroked-button
          type="button"
          (click)="refreshRequested.emit()">
          <span i18n="Button that reloads the roadmap">Refresh</span>
        </button>
      </div>
    </div>
  `,
})
export class RoadmapFiltersComponent {
  readonly from = input.required<string>();
  readonly to = input.required<string>();
  readonly zoom = input.required<TimelineZoom>();
  readonly projectId = input<number>();
  readonly projects = input<ProjectViewModel[]>([]);
  readonly sprintId = input<number>();
  readonly sprints = input<SprintOption[]>([]);
  readonly includeUnscheduled = input.required<boolean>();

  readonly fromChanged = output<string>();
  readonly toChanged = output<string>();
  readonly zoomChanged = output<TimelineZoom>();
  readonly projectChanged = output<number | null>();
  readonly sprintChanged = output<number | null>();
  readonly includeUnscheduledChanged = output<boolean>();
  readonly todayRequested = output();
  readonly rangeNavigationRequested = output<-1 | 1>();
  readonly refreshRequested = output();

  protected readonly zoomOptions: readonly ZoomOption[] = [
    { label: $localize`:Label shown in the interface:Day`, value: 'day' },
    { label: $localize`:Label shown in the interface:Week`, value: 'week' },
    { label: $localize`:Label shown in the interface:Month`, value: 'month' },
  ];

  protected zoomLabel(): string {
    return `Zoom: ${this.zoomOptions.find((option) => option.value === this.zoom())?.label ?? 'Week'}`;
  }

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
