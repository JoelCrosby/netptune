import { Component, computed } from '@angular/core';
import { taskFilterRoute } from '@core/router/task-filter-route';
import { LucideFlag } from '@lucide/angular';

@Component({
  selector: 'app-task-list-flags',
  imports: [LucideFlag],
  template: `
    <button
      type="button"
      class="border-border hover:bg-foreground/5 flex h-9 items-center gap-2 rounded-md border px-3 text-sm transition-colors"
      [class.border-amber-400]="selected()"
      [class.bg-amber-400/10]="selected()"
      [class.text-amber-700]="selected()"
      [attr.aria-pressed]="selected()"
      (click)="toggle()">
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
