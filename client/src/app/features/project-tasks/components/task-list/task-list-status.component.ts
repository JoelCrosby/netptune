import { Component, computed } from '@angular/core';
import { statusResource } from '@app/core/resources/status.resources';
import { taskFilterRoute } from '@core/router/task-filter-route';
import { StatusFilterComponent } from '@static/components/status-filter/status-filter.component';

@Component({
  selector: 'app-task-list-status',
  imports: [StatusFilterComponent],
  template: `
    <app-status-filter
      [statuses]="statuses.value()"
      [selected]="selected()"
      [selectedCount]="selectedCount()"
      (toggled)="onToggled($event)" />
  `,
})
export class TaskListStatusComponent {
  private readonly filterRoute = taskFilterRoute();

  readonly statuses = statusResource();

  readonly selected = computed(
    () => new Set(this.filterRoute.filters().statuses ?? [])
  );

  readonly selectedCount = computed(() => this.selected().size);

  onToggled(status: number) {
    const selected = new Set(this.selected());

    if (selected.has(status)) {
      selected.delete(status);
    } else {
      selected.add(status);
    }

    this.filterRoute.set('statusIds', [...selected]);
  }
}
