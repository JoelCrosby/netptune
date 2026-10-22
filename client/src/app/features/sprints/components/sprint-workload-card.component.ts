import { Component, input } from '@angular/core';
import { LucideUsers } from '@lucide/angular';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import {
  MeterEntry,
  MeterListComponent,
} from '@static/components/meter-list/meter-list.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';

@Component({
  selector: 'app-sprint-workload-card',
  imports: [
    ChartCardComponent,
    EmptyStateComponent,
    LucideUsers,
    MeterListComponent,
    StatStripComponent,
  ],
  host: { class: 'block h-full' },
  template: `
    <app-chart-card
      [icon]="workloadIcon"
      i18n-title="Heading of the sprint workload card"
      title="Workload"
      i18n-description="Explains what the sprint workload card counts"
      description="Open tasks per assignee">
      @if (entries().length) {
        <app-meter-list [entries]="entries()" />
        <div class="-mx-6 mt-auto -mb-5 pt-5">
          <app-stat-strip [items]="stats()" />
        </div>
      } @else {
        <app-empty-state
          compact
          i18n-title="Shown when a sprint has no open tasks"
          title="No open tasks."
          i18n-description="Explains why the sprint workload card is empty"
          description="Everything in this sprint is complete.">
          <svg emptyStateIcon lucideUsers class="h-8 w-8"></svg>
        </app-empty-state>
      }
    </app-chart-card>
  `,
})
export class SprintWorkloadCardComponent {
  readonly entries = input.required<readonly MeterEntry[]>();
  readonly stats = input<readonly StatStripItem[]>([]);

  protected readonly workloadIcon = LucideUsers;
}
