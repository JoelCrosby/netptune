import {
  Component,
  computed,
  input,
  output,
  signal,
  Signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Status } from '@core/models/status';
import {
  LucideCirclePlay,
  LucideCopy,
  LucidePencil,
  LucideTrash2,
} from '@lucide/angular';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import {
  automationRunStatusLabels,
  AutomationCopySegment,
  describeAutomationActionsSegments,
  describeAutomationTriggerSegments,
  runStatusClass,
} from '../models/automation-copy';
import {
  AutomationRuleListItem,
  AutomationRunStatus,
} from '../models/automation.models';
import { AutomationDescriptionComponent } from './automation-description.component';
import { AutomationEnabledBadgeComponent } from './automation-enabled-badge.component';

@Component({
  selector: 'app-automation-rules-table',
  imports: [
    RouterLink,
    DatatableComponent,
    DatatableCellTemplateDirective,
    EmptyStateComponent,
    PrettyDatePipe,
    AutomationEnabledBadgeComponent,
    AutomationDescriptionComponent,
  ],
  template: `
    <app-datatable
      containerClass="max-h-[calc(100vh-360px)] min-h-80 overflow-auto"
      tableClass="min-w-[900px]"
      rowClass="bg-card"
      [data]="data()"
      [stickyHeader]="true">
      <ng-template appDatatableCell="name" let-rule>
        <a
          class="block truncate font-semibold hover:underline"
          [routerLink]="[rule.id]">
          {{ rule.name }}
        </a>
      </ng-template>

      <ng-template appDatatableCell="isEnabled" let-rule>
        <app-automation-enabled-badge [enabled]="rule.isEnabled" />
      </ng-template>

      <ng-template appDatatableCell="trigger" let-rule>
        <app-automation-description
          [segments]="triggerSummary(rule)"
          [statuses]="statuses()" />
      </ng-template>

      <ng-template appDatatableCell="actions" let-rule>
        <app-automation-description
          [segments]="actionsSummary(rule)"
          [statuses]="statuses()" />
      </ng-template>

      <ng-template appDatatableCell="lastRun" let-rule>
        @if (rule.lastRun) {
          <div class="flex flex-col gap-1">
            <span class="font-mono text-xs whitespace-nowrap">
              {{ rule.lastRun.createdAt | prettyDate }}
            </span>
            <span
              class="w-fit rounded px-2 py-0.5 text-xs font-medium"
              [class]="runStatusClass(rule.lastRun.status)">
              {{ runStatusLabel(rule.lastRun.status) }}
            </span>
          </div>
        } @else {
          <span class="text-muted text-xs">Not run yet</span>
        }
      </ng-template>

      <app-empty-state
        appDatatableEmpty
        compact
        title="No automations match this view"
        description="Create an automation or clear the current filters." />
    </app-datatable>
  `,
})
export class AutomationRulesTableComponent {
  readonly canManage = input.required<boolean>();
  readonly statuses = input<Status[]>([]);
  readonly reloadSignal = input<Signal<unknown>>();

  readonly toggleRule = output<AutomationRuleListItem>();
  readonly editRule = output<AutomationRuleListItem>();
  readonly cloneRule = output<AutomationRuleListItem>();
  readonly deleteRule = output<AutomationRuleListItem>();

  readonly runStatusClass = runStatusClass;

  readonly data = computed<DatatableDataSource<AutomationRuleListItem>>(() => ({
    key: 'automation-rules',
    columns: [
      { id: 'name', header: 'Rule', sortable: true, widthClass: 'w-64' },
      {
        id: 'isEnabled',
        header: 'Status',
        sortable: true,
        widthClass: 'w-28',
      },
      { id: 'trigger', header: 'Trigger', cellClass: 'min-w-0' },
      { id: 'actions', header: 'Actions', cellClass: 'min-w-0' },
      {
        id: 'lastRun',
        header: 'Last run',
        widthClass: 'w-44',
      },
    ],
    resource: {
      url: 'api/automations',
      params: signal({}),
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, rule: AutomationRuleListItem) => rule.id,
    menu: this.canManage() ? this.rowMenu() : undefined,
    reloadSignal: this.reloadSignal(),
  }));

  triggerSummary(rule: AutomationRuleListItem): AutomationCopySegment[] {
    return describeAutomationTriggerSegments(rule.trigger, this.statuses());
  }

  actionsSummary(rule: AutomationRuleListItem): AutomationCopySegment[] {
    return describeAutomationActionsSegments(rule.actions, this.statuses());
  }

  runStatusLabel(status: AutomationRunStatus): string {
    return automationRunStatusLabels[status];
  }

  private rowMenu() {
    return [
      {
        label: 'Edit rule',
        icon: LucidePencil,
        onClick: (rule: AutomationRuleListItem) => this.editRule.emit(rule),
      },
      {
        label: 'Enable or disable rule',
        icon: LucideCirclePlay,
        onClick: (rule: AutomationRuleListItem) => this.toggleRule.emit(rule),
      },
      {
        label: 'Clone rule',
        icon: LucideCopy,
        onClick: (rule: AutomationRuleListItem) => this.cloneRule.emit(rule),
      },
      {
        label: 'Delete rule',
        icon: LucideTrash2,
        onClick: (rule: AutomationRuleListItem) => this.deleteRule.emit(rule),
      },
    ];
  }
}
