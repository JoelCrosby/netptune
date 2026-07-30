import { Component, input } from '@angular/core';
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
import {
  TableComponent,
  TableEmptyCellDirective,
  TableHeaderRowDirective,
  TableHeadDirective,
  TableRowDirective,
} from '@static/components/table/table.component';

@Component({
  selector: 'app-automation-runs-table',
  imports: [
    PrettyDatePipe,
    TableComponent,
    TableEmptyCellDirective,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
  ],
  template: `
    <app-table>
      <thead appTableHead>
        <tr appTableHeaderRow>
          <th class="px-4 py-3" i18n="Column heading for the run time">Time</th>
          <th class="px-4 py-3">
            <span i18n="Column heading for the trigger">Trigger</span>
          </th>
          <th class="px-4 py-3">
            <span i18n="Column heading for the task a run acted on">
              Target
            </span>
          </th>
          <th class="px-4 py-3">
            <span i18n="Column heading for the run status">Status</span>
          </th>
          <th class="px-4 py-3">
            <span i18n="Column heading for the run result">Result</span>
          </th>
        </tr>
      </thead>
      <tbody>
        @for (run of runs(); track run.id) {
          <tr appTableRow>
            <td
              class="text-foreground/70 px-4 py-2.5 font-mono text-xs whitespace-nowrap">
              {{ run.createdAt | prettyDate }}
            </td>
            <td class="px-4 py-2.5">
              {{ triggerLabel(run.triggerType) }}
            </td>
            <td class="text-foreground/70 px-4 py-2.5">
              {{ targetLabel(run) }}
            </td>
            <td class="px-4 py-2.5">
              <span
                [class]="
                  'rounded px-2 py-0.5 text-xs font-medium ' +
                  statusClass(run.status)
                ">
                {{ statusLabel(run.status) }}
              </span>
            </td>
            <td class="text-foreground/70 max-w-96 px-4 py-2.5">
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
                        [class]="
                          'shrink-0 rounded px-1.5 py-0.5 font-medium ' +
                          actionStatusClass(result.status)
                        ">
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
            </td>
          </tr>
        } @empty {
          <tr>
            <td appTableEmptyCell colspan="5">
              <span i18n="Empty state for the run history">
                No automation runs recorded yet.
              </span>
            </td>
          </tr>
        }
      </tbody>
    </app-table>
  `,
})
export class AutomationRunsTableComponent {
  readonly runs = input<AutomationRun[]>([]);

  statusClass = runStatusClass;

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
