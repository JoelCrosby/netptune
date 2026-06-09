import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import {
  automationRunStatusLabels,
  entityTargetLabel,
  runStatusClass,
  triggerTypeLabels,
} from '../models/automation-copy';
import { AutomationRun } from '../models/automation.models';
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
  changeDetection: ChangeDetectionStrategy.OnPush,
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
          <th class="px-4 py-3">Time</th>
          <th class="px-4 py-3">Trigger</th>
          <th class="px-4 py-3">Target</th>
          <th class="px-4 py-3">Status</th>
          <th class="px-4 py-3">Result</th>
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
              <span class="line-clamp-2">
                {{ run.message || 'No message recorded' }}
              </span>
            </td>
          </tr>
        } @empty {
          <tr>
            <td appTableEmptyCell colspan="5">
              No automation runs recorded yet.
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
