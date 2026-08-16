import { Component, computed, inject } from '@angular/core';
import { statusResource } from '@app/core/resources/status.resource';
import { TaskFilterService } from '@core/services/task-filter.service';
import { StatusFilterComponent } from '@static/components/status-filter/status-filter.component';

@Component({
  selector: 'app-board-group-status',
  imports: [StatusFilterComponent],
  template: `
    <app-status-filter
      [statuses]="statuses.value()"
      [selected]="selected()"
      [selectedCount]="selectedCount()"
      (toggled)="onToggled($event)" />
  `,
})
export class BoardGroupStatusComponent {
  private readonly filters = inject(TaskFilterService);

  readonly statuses = statusResource();

  private readonly selectedIds = computed(
    () => this.filters.filters().statuses ?? []
  );

  readonly selected = computed(() => new Set(this.selectedIds()));
  readonly selectedCount = computed(() => this.selectedIds().length);

  onToggled(status: number) {
    const selected = this.selectedIds();

    const statuses = selected.includes(status)
      ? selected.filter((id) => id !== status)
      : [...selected, status];

    this.filters.update({ statuses });
  }
}
