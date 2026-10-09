import { Component, computed } from '@angular/core';
import { taskFilterRoute } from '@core/router/task-filter-route';
import { LucideFlag } from '@lucide/angular';
import { FilterToggleComponent } from '@static/components/filter-toggle/filter-toggle.component';

@Component({
  selector: 'app-task-list-flags',
  imports: [FilterToggleComponent, LucideFlag],
  template: `
    <button app-filter-toggle [pressed]="selected()" (click)="toggle()">
      <svg lucideFlag size="16" aria-hidden="true"></svg>
      <span i18n="Indicates a task has one or more flags raised against it">
        Flagged
      </span>
    </button>
  `,
})
export class TaskListFlagsComponent {
  private readonly filterRoute = taskFilterRoute();

  readonly selected = computed(
    () => this.filterRoute.filters().hasFlags === true
  );

  toggle() {
    this.filterRoute.set('hasFlags', this.selected() ? null : true);
  }
}
