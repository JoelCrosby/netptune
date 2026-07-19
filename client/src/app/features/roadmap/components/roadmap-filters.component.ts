import { Component, input, output } from '@angular/core';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { LucideCheck } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DateDropdownButtonComponent } from '@static/components/dropdown-menu/date-dropdown-button.component';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { TimelineZoom } from '@static/components/timeline/timeline.models';

interface ZoomOption {
  label: string;
  value: TimelineZoom;
}

@Component({
  selector: 'app-roadmap-filters',
  imports: [
    DateDropdownButtonComponent,
    DropdownButtonComponent,
    LucideCheck,
    MenuItemComponent,
    StrokedButtonComponent,
  ],
  template: `
    <div
      class="border-border flex flex-wrap items-center gap-2 border-b p-3"
      aria-label="Roadmap filters">
      <app-date-dropdown-button
        label="From"
        ariaLabel="Roadmap start date"
        buttonClass="min-w-44 justify-between"
        [value]="from()"
        (valueChanged)="fromChanged.emit($event)" />

      <app-date-dropdown-button
        label="To"
        ariaLabel="Roadmap end date"
        buttonClass="min-w-44 justify-between"
        [value]="to()"
        (valueChanged)="toChanged.emit($event)" />

      <app-dropdown-button
        #zoomMenu
        [label]="zoomLabel()"
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
        #projectMenu
        [label]="projectLabel()"
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
          <span>All projects</span>
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

      <label class="flex cursor-pointer items-center gap-2 px-2 text-sm">
        <input
          type="checkbox"
          class="h-4 w-4"
          [checked]="includeUnscheduled()"
          (change)="changeUnscheduled($event)" />
        <span>Show unscheduled</span>
      </label>

      <div class="ml-auto flex items-center gap-2">
        <button
          app-stroked-button
          color="neutral"
          type="button"
          (click)="todayRequested.emit()">
          Today
        </button>
        <button
          app-stroked-button
          type="button"
          (click)="refreshRequested.emit()">
          Refresh
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
  readonly includeUnscheduled = input.required<boolean>();

  readonly fromChanged = output<string>();
  readonly toChanged = output<string>();
  readonly zoomChanged = output<TimelineZoom>();
  readonly projectChanged = output<number | null>();
  readonly includeUnscheduledChanged = output<boolean>();
  readonly todayRequested = output();
  readonly refreshRequested = output();

  protected readonly zoomOptions: readonly ZoomOption[] = [
    { label: 'Day', value: 'day' },
    { label: 'Week', value: 'week' },
    { label: 'Month', value: 'month' },
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

  protected changeUnscheduled(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.includeUnscheduledChanged.emit(checked);
  }
}
