import {
  Component,
  computed,
  input,
  output,
  signal,
  Signal,
  viewChild,
} from '@angular/core';
import { debouncedSignal, onChange } from '@core/util/signals';
import { prettyDate, prettyShortDate } from '@core/util/dates';
import { Params, RouterLink } from '@angular/router';
import { Status } from '@core/models/status';
import {
  LucideCopy,
  LucideListFilter,
  LucideSettings2,
  LucideTrash2,
  LucideTriangleAlert,
  LucideZap,
} from '@lucide/angular';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { SwitchComponent } from '@static/components/switch/switch.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import {
  AutomationActionChip,
  automationRunStatusLabels,
  describeAutomationActionChip,
  describeAutomationConditionChip,
  describeAutomationTrigger,
  describeAutomationTriggerChip,
} from '../models/automation-copy';
import {
  AutomationRuleListItem,
  AutomationRun,
  AutomationRunStatus,
} from '../models/automation.models';

export interface AutomationRuleToggle {
  rule: AutomationRuleListItem;
  revert: () => void;
}

export interface AutomationRuleCounts {
  total: number;
  enabled: number;
}

type EnabledFilter = 'all' | 'enabled' | 'disabled';

interface ActionChips {
  shown: AutomationActionChip[];
  extra: AutomationActionChip[];
}

const maxActionChips = 2;

const chipClass =
  'border-border bg-foreground/[0.015] text-foreground/75 inline-flex h-6.5 min-w-0 items-center gap-1.5 rounded-md border px-2.25 text-[13px] whitespace-nowrap';

@Component({
  selector: 'app-automation-rules-table',
  imports: [
    RouterLink,
    DatatableComponent,
    DatatableCellTemplateDirective,
    DatatableEmptyDirective,
    EmptyStateComponent,
    SearchInputComponent,
    SegmentedControlComponent,
    SwitchComponent,
    LucideListFilter,
    LucideTriangleAlert,
    LucideZap,
    TooltipDirective,
  ],
  template: `
    <div class="mb-3 flex flex-row flex-wrap items-center gap-3">
      <app-search-input
        [term]="searchInput()"
        (searchChange)="searchInput.set($event ?? '')" />

      <app-segmented-control
        variant="outlined"
        i18n-ariaLabel="Accessible label of the automation status filter"
        ariaLabel="Filter by status"
        [options]="enabledOptions()"
        [value]="enabledFilter()"
        (valueChange)="setEnabledFilter($event)" />
    </div>

    <app-datatable
      i18n-errorMessage="Shown when the automation list fails to load"
      errorMessage="Automation rules could not be loaded."
      stickyHeader
      tableClass="table-fixed md:min-w-[900px]"
      [data]="data()"
      [stickyHeader]="true">
      <ng-template appDatatableCell="name" let-rule>
        <div class="flex min-w-0 items-center gap-2">
          <a
            class="min-w-0 truncate font-semibold hover:underline"
            [class.text-muted]="!rule.isEnabled"
            [routerLink]="[rule.id]"
            [appTooltip]="rule.name">
            {{ rule.name }}
          </a>
          @if (rule.warnings.length) {
            <svg
              lucideTriangleAlert
              class="text-warn h-4 w-4 shrink-0"
              [appTooltip]="warningTooltip(rule)"></svg>
          }
        </div>
      </ng-template>

      <ng-template appDatatableCell="isEnabled" let-rule>
        <app-switch
          #toggle
          [checked]="rule.isEnabled"
          [disabled]="!canManage()"
          [ariaLabel]="toggleLabel(rule)"
          [appTooltip]="toggleLabel(rule)"
          (changed)="onToggle(rule, toggle)" />
      </ng-template>

      <ng-template appDatatableCell="trigger" let-rule>
        @let condition = conditionChip(rule);
        <div class="flex min-w-0 items-center gap-1.5 overflow-hidden">
          <span
            [class]="chipClass + ' max-w-full shrink-0'"
            [appTooltip]="triggerTooltip(rule)">
            <svg
              lucideZap
              class="text-primary h-3.25 w-3.25 shrink-0"
              [strokeWidth]="2.2"></svg>
            <span class="min-w-0 truncate">{{ triggerChip(rule) }}</span>
          </span>
          @if (condition) {
            <span [class]="chipClass" [appTooltip]="condition">
              <svg
                lucideListFilter
                class="text-foreground/50 h-3.25 w-3.25 shrink-0"
                [strokeWidth]="2.2"></svg>
              <span class="min-w-0 truncate">{{ condition }}</span>
            </span>
          }
        </div>
      </ng-template>

      <ng-template appDatatableCell="actions" let-rule>
        @let chips = actionChips(rule);
        <div class="flex min-w-0 items-center gap-1.5 overflow-hidden">
          @for (chip of chips.shown; track $index; let last = $last) {
            <span
              [class]="chipClass"
              [style.max-width]="last ? null : '150px'"
              [appTooltip]="chip.full">
              <span class="text-foreground shrink-0 font-semibold">
                {{ chip.verb }}
              </span>
              @if (chip.detail) {
                <span class="text-foreground/55 min-w-0 truncate">
                  {{ chip.detail }}
                </span>
              }
            </span>
          } @empty {
            <span
              class="text-muted text-xs"
              i18n="Shown when an automation has no actions">
              No actions configured
            </span>
          }
          @if (chips.extra.length) {
            <span
              class="bg-foreground/5 text-foreground/60 inline-flex h-6.5 shrink-0 items-center rounded-md px-2 text-xs font-semibold"
              [appTooltip]="extraActionsTooltip(chips.extra)">
              +{{ chips.extra.length }}
            </span>
          }
        </div>
      </ng-template>

      <ng-template appDatatableCell="lastRun" let-rule>
        @if (rule.lastRun; as run) {
          <div
            class="flex min-w-0 items-center gap-2 whitespace-nowrap"
            [appTooltip]="runTooltip(run)">
            <span
              class="h-1.75 w-1.75 shrink-0 rounded-full"
              [class]="runDotClass(run.status)"></span>
            <span class="text-foreground/75 truncate font-mono text-xs">
              {{ shortDate(run.createdAt) }}
            </span>
          </div>
        } @else {
          <span
            class="text-muted text-xs"
            i18n="Shown when an automation has never run">
            Not run yet
          </span>
        }
      </ng-template>

      <ng-template appDatatableEmpty>
        <app-empty-state
          compact
          [title]="emptyTitle()"
          [description]="emptyDescription()" />
      </ng-template>
    </app-datatable>
  `,
})
export class AutomationRulesTableComponent {
  readonly canManage = input.required<boolean>();
  readonly statuses = input<Status[]>([]);
  readonly counts = input<AutomationRuleCounts>({ total: 0, enabled: 0 });
  readonly reloadSignal = input<Signal<unknown>>();

  readonly toggleRule = output<AutomationRuleToggle>();
  readonly editRule = output<AutomationRuleListItem>();
  readonly cloneRule = output<AutomationRuleListItem>();
  readonly deleteRule = output<AutomationRuleListItem>();

  readonly chipClass = chipClass;

  readonly searchInput = signal('');
  readonly enabledFilter = signal<EnabledFilter>('all');

  private readonly search = debouncedSignal(this.searchInput);

  private readonly datatable = viewChild(
    DatatableComponent<AutomationRuleListItem>
  );

  readonly enabledOptions = computed<SegmentedOption<EnabledFilter>[]>(() => {
    const { total, enabled } = this.counts();

    return [
      {
        value: 'all',
        label: $localize`:Automation status filter option that shows every rule:All`,
        count: total,
      },
      {
        value: 'enabled',
        label: $localize`:Marks an automation that is switched on:Enabled`,
        count: enabled,
      },
      {
        value: 'disabled',
        label: $localize`:Marks an automation that is switched off:Disabled`,
        count: total - enabled,
      },
    ];
  });

  readonly filtersActive = computed(() => {
    return !!this.search().trim() || this.enabledFilter() !== 'all';
  });

  readonly emptyTitle = computed(() => {
    if (this.filtersActive()) {
      return $localize`:Shown when no automations match the active filters:No automations match these filters`;
    }

    return $localize`:Heading of the empty automation list:No automations yet`;
  });

  readonly emptyDescription = computed(() => {
    if (!this.filtersActive()) return '';

    return $localize`:Advice shown when filters exclude every row:Try a different search or filter.`;
  });

  private readonly resourceParams = computed<Params>(() => {
    const search = this.search().trim();
    const filter = this.enabledFilter();

    return {
      ...(search ? { search } : {}),
      ...(filter === 'all' ? {} : { isEnabled: filter === 'enabled' }),
    };
  });

  readonly data = computed<DatatableDataSource<AutomationRuleListItem>>(() => ({
    key: 'automation-rules',
    columns: [
      {
        id: 'name',
        header: 'Rule',
        visibleOnMobile: true,
        sortable: true,
      },
      {
        id: 'isEnabled',
        header: 'Status',
        visibleOnMobile: true,
        sortable: true,
        widthClass: 'w-32',
      },
      {
        id: 'trigger',
        header: 'Trigger',
        visibleOnMobile: false,
        cellClass: 'min-w-0',
      },
      {
        id: 'actions',
        header: 'Actions',
        visibleOnMobile: false,
        cellClass: 'min-w-0',
      },
      {
        id: 'lastRun',
        header: 'Last run',
        visibleOnMobile: false,
        widthClass: 'w-43',
      },
    ],
    resource: {
      url: 'api/automations',
      params: this.resourceParams,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, rule: AutomationRuleListItem) => rule.id,
    menu: this.canManage() ? this.rowMenu() : undefined,
    reloadSignal: this.reloadSignal(),
  }));

  constructor() {
    onChange(this.search, () => this.goToFirstPage());
  }

  setEnabledFilter(filter: EnabledFilter) {
    this.enabledFilter.set(filter);
    this.goToFirstPage();
  }

  onToggle(rule: AutomationRuleListItem, toggle: SwitchComponent) {
    this.toggleRule.emit({
      rule,
      revert: () => toggle.checked.set(rule.isEnabled),
    });
  }

  private goToFirstPage() {
    this.datatable()?.goToPage(1);
  }

  toggleLabel(rule: AutomationRuleListItem): string {
    return rule.isEnabled
      ? $localize`:Tooltip on the switch that turns an automation off:Disable automation`
      : $localize`:Tooltip on the switch that turns an automation on:Enable automation`;
  }

  triggerChip(rule: AutomationRuleListItem): string {
    return describeAutomationTriggerChip(rule.trigger);
  }

  triggerTooltip(rule: AutomationRuleListItem): string {
    return describeAutomationTrigger(rule.trigger, this.statuses());
  }

  conditionChip(rule: AutomationRuleListItem): string | null {
    return describeAutomationConditionChip(rule.trigger, this.statuses());
  }

  actionChips(rule: AutomationRuleListItem): ActionChips {
    const chips = rule.actions.map((action) => {
      return describeAutomationActionChip(action, this.statuses());
    });

    return {
      shown: chips.slice(0, maxActionChips),
      extra: chips.slice(maxActionChips),
    };
  }

  extraActionsTooltip(chips: AutomationActionChip[]): string {
    return chips.map((chip) => chip.full).join('\n');
  }

  warningTooltip(rule: AutomationRuleListItem): string {
    return rule.warnings.map((warning) => warning.message).join('\n');
  }

  shortDate(value: Date): string {
    return prettyShortDate(value);
  }

  runTooltip(run: AutomationRun): string {
    return `${automationRunStatusLabels[run.status]} · ${prettyDate(run.createdAt)}`;
  }

  runDotClass(status: AutomationRunStatus): string {
    switch (status) {
      case AutomationRunStatus.succeeded:
        return 'bg-green-600 dark:bg-green-400';
      case AutomationRunStatus.failed:
        return 'bg-warn';
      case AutomationRunStatus.skipped:
        return 'bg-amber-500';
    }
  }

  private rowMenu() {
    return [
      {
        label: $localize`:Row action that edits an automation:Edit rule`,
        icon: LucideSettings2,
        onClick: (rule: AutomationRuleListItem) => this.editRule.emit(rule),
      },
      {
        label: $localize`:Row action that duplicates an automation:Clone rule`,
        icon: LucideCopy,
        onClick: (rule: AutomationRuleListItem) => this.cloneRule.emit(rule),
      },
      {
        label: $localize`:Row action that deletes an automation:Delete rule`,
        icon: LucideTrash2,
        onClick: (rule: AutomationRuleListItem) => this.deleteRule.emit(rule),
      },
    ];
  }
}
