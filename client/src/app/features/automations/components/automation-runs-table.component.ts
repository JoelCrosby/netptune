import { Component, computed, input, signal, Signal } from '@angular/core';
import { Params } from '@angular/router';
import {
  actionTypeLabels,
  automationActionResultStatusLabels,
  automationRunStatusLabels,
  entityTargetLabel,
  runStatusClass,
  triggerTypeLabels,
} from '../models/automation-copy';
import {
  AutomationActionResult,
  AutomationActionResultStatus,
  AutomationRun,
} from '../models/automation.models';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableDataSource,
  DatatableSort,
} from '@static/components/datatable/datatable.types';

@Component({
  selector: 'app-automation-runs-table',
  imports: [PrettyDatePipe, DatatableComponent, DatatableCellTemplateDirective],
  template: `
    <app-datatable
      i18n-errorMessage="Shown when the automation run history fails to load"
      errorMessage="Automation runs could not be loaded."
      i18n-emptyMessage="Empty state for the run history"
      emptyMessage="No automation runs recorded yet."
      i18n-itemLabel="Plural noun for automation runs, used by the paginator"
      itemLabel="runs"
      containerClass="overflow-x-auto"
      tableClass="min-w-[900px]"
      [data]="data()"
      [sort]="sort()"
      (sortChange)="sort.set($event)">
      <ng-template appDatatableCell="createdAt" let-run>
        <span class="text-foreground/70 font-mono text-xs whitespace-nowrap">
          {{ run.createdAt | prettyDate }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="triggerType" let-run>
        {{ triggerLabel(run.triggerType) }}
      </ng-template>

      <ng-template appDatatableCell="target" let-run>
        <span class="text-foreground/70">{{ targetLabel(run) }}</span>
      </ng-template>

      <ng-template appDatatableCell="status" let-run>
        <span
          class="rounded px-2 py-0.5 text-xs font-medium"
          [class]="statusClass(run.status)">
          {{ statusLabel(run.status) }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="result" let-run>
        <div class="text-foreground/70">
          @if (run.message) {
            <p class="mb-2 text-xs">{{ run.message }}</p>
          }
          @if (run.actionResults.length) {
            <ol
              class="space-y-1.5"
              i18n-aria-label="Accessible name of the action result list"
              aria-label="Action results">
              @for (result of run.actionResults; track result.id) {
                <li class="flex items-start gap-2 text-xs">
                  <span
                    class="bg-foreground/5 text-foreground/60 mt-0.5 flex size-5 shrink-0 items-center justify-center rounded-full font-mono">
                    {{ $index + 1 }}
                  </span>
                  <span class="min-w-0 flex-1">
                    <span class="text-foreground block font-medium">
                      {{ actionLabel(result) }}
                    </span>
                    @if (result.message) {
                      <span class="block">{{ result.message }}</span>
                    }
                  </span>
                  <span
                    class="shrink-0 rounded px-1.5 py-0.5 font-medium"
                    [class]="actionStatusClass(result.status)">
                    {{ actionStatusLabel(result.status) }}
                    @if (durationLabel(result); as duration) {
                      · {{ duration }}
                    }
                  </span>
                </li>
              }
            </ol>
          } @else {
            <span
              class="text-xs"
              i18n="Shown when a run recorded no action results">
              No action results recorded
            </span>
          }
        </div>
      </ng-template>
    </app-datatable>
  `,
})
export class AutomationRunsTableComponent {
  readonly ruleId = input.required<number>();
  readonly reloadSignal = input<Signal<unknown>>();

  readonly sort = signal<DatatableSort | null>({
    sortBy: 'createdAt',
    sortDirection: 'desc',
  });

  statusClass = runStatusClass;

  private readonly resourceParams = computed<Params>(() => ({}));

  readonly data = computed<DatatableDataSource<AutomationRun>>(() => ({
    key: 'automation-runs',
    columns: [
      { id: 'createdAt', header: 'Time', sortable: true, widthClass: 'w-44' },
      {
        id: 'triggerType',
        header: 'Trigger',
        sortable: true,
        widthClass: 'w-48',
      },
      { id: 'target', header: 'Target', widthClass: 'w-40' },
      { id: 'status', header: 'Status', sortable: true, widthClass: 'w-32' },
      { id: 'result', header: 'Result', cellClass: 'max-w-96' },
    ],
    resource: {
      url: `api/automations/${this.ruleId()}/runs`,
      params: this.resourceParams,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, run: AutomationRun) => run.id,
    reloadSignal: this.reloadSignal(),
  }));

  actionLabel(result: AutomationActionResult): string {
    return actionTypeLabels[result.actionType];
  }

  actionStatusLabel(status: AutomationActionResultStatus): string {
    return automationActionResultStatusLabels[status];
  }

  actionStatusClass(status: AutomationActionResultStatus): string {
    switch (status) {
      case AutomationActionResultStatus.succeeded:
        return 'bg-green-500/10 text-green-600 dark:text-green-400';
      case AutomationActionResultStatus.failed:
        return 'bg-red-500/10 text-red-600 dark:text-red-400';
      case AutomationActionResultStatus.skipped:
        return 'bg-foreground/5 text-foreground/60';
      case AutomationActionResultStatus.scheduled:
        return 'bg-blue-500/10 text-blue-600 dark:text-blue-400';
      case AutomationActionResultStatus.pending:
        return 'bg-amber-500/10 text-amber-600 dark:text-amber-400';
    }
  }

  durationLabel(result: AutomationActionResult): string {
    if (!result.startedAt || !result.completedAt) {
      return '';
    }

    const startedAt = new Date(result.startedAt).getTime();
    const completedAt = new Date(result.completedAt).getTime();
    const durationMs = Math.max(0, completedAt - startedAt);

    if (durationMs < 1000) {
      return `${durationMs} ms`;
    }

    return `${(durationMs / 1000).toFixed(1)} s`;
  }

  triggerLabel(triggerType: AutomationRun['triggerType']): string {
    return triggerTypeLabels[triggerType];
  }

  statusLabel(status: AutomationRun['status']): string {
    return automationRunStatusLabels[status];
  }

  targetLabel(run: AutomationRun): string {
    return entityTargetLabel(run.entityType, run.entityId);
  }
}
