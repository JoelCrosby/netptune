import {
  Component,
  computed,
  effect,
  input,
  output,
  signal,
  Signal,
  viewChild,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Params, RouterLink } from '@angular/router';
import { Status } from '@core/models/status';
import {
  LucideCircleDashed,
  LucideCirclePlay,
  LucideCopy,
  LucideSettings2,
  LucideTrash2,
  LucideZap,
} from '@lucide/angular';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuCheckboxItemComponent } from '@static/components/dropdown-menu/menu-checkbox-item.component';
import { FilterActionButtonComponent } from '@static/components/filter-action-button/filter-action-button.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { debounceTime } from 'rxjs/operators';
import {
  automationRunStatusLabels,
  AutomationCopySegment,
  triggerTypeLabels,
  describeAutomationActionsSegments,
  describeAutomationTriggerSegments,
  runStatusClass,
} from '../models/automation-copy';
import {
  AutomationRuleListItem,
  AutomationRunStatus,
  AutomationTriggerType,
} from '../models/automation.models';
import { AutomationDescriptionComponent } from './automation-description.component';
import { AutomationEnabledBadgeComponent } from './automation-enabled-badge.component';

@Component({
  selector: 'app-automation-rules-table',
  imports: [
    RouterLink,
    DatatableComponent,
    DatatableCellTemplateDirective,
    DropdownMenuComponent,
    EmptyStateComponent,
    FilterActionButtonComponent,
    MenuCheckboxItemComponent,
    PrettyDatePipe,
    SearchInputComponent,
    AutomationEnabledBadgeComponent,
    AutomationDescriptionComponent,
  ],
  template: `
    <div class="mb-3 flex flex-row items-center gap-2">
      <app-search-input
        [term]="searchInput()"
        (searchChange)="searchInput.set($event ?? '')" />

      <div #statusAnchor>
        <app-filter-action-button
          label="Filter by Status"
          [icon]="lucideCircleDashed"
          [color]="enabledFilter().size ? 'primary' : undefined"
          [count]="enabledFilter().size"
          (action)="statusMenu.toggle(statusAnchor)" />
      </div>

      <app-dropdown-menu #statusMenu>
        <button
          app-menu-checkbox-item
          [checked]="enabledFilter().has(true)"
          (checkedChange)="toggleEnabledFilter(true)">
          Enabled
        </button>
        <button
          app-menu-checkbox-item
          [checked]="enabledFilter().has(false)"
          (checkedChange)="toggleEnabledFilter(false)">
          Disabled
        </button>
      </app-dropdown-menu>

      <div #triggerAnchor>
        <app-filter-action-button
          label="Filter by Trigger"
          [icon]="lucideZap"
          [color]="triggerFilter().size ? 'primary' : undefined"
          [count]="triggerFilter().size"
          (action)="triggerMenu.toggle(triggerAnchor)" />
      </div>

      <app-dropdown-menu #triggerMenu>
        @for (trigger of triggerTypes; track trigger) {
          <button
            app-menu-checkbox-item
            [checked]="triggerFilter().has(trigger)"
            (checkedChange)="toggleTriggerFilter(trigger)">
            {{ triggerLabel(trigger) }}
          </button>
        }
      </app-dropdown-menu>
    </div>

    <app-datatable
      errorMessage="Automation rules could not be loaded."
      containerClass="max-h-[calc(100vh-420px)] min-h-80 overflow-auto"
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
        [title]="
          filtersActive()
            ? 'No automations match these filters'
            : 'No automations yet'
        "
        [description]="
          filtersActive() ? 'Try a different search or filter.' : ''
        " />
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
  readonly triggerTypes = [
    AutomationTriggerType.taskChanged,
    AutomationTriggerType.taskCreated,
    AutomationTriggerType.taskUnassignedFor,
    AutomationTriggerType.taskDueDateApproaching,
    AutomationTriggerType.taskOverdue,
    AutomationTriggerType.taskHasNoDueDate,
    AutomationTriggerType.taskInactiveFor,
  ];

  readonly lucideCircleDashed = LucideCircleDashed;
  readonly lucideZap = LucideZap;

  readonly searchInput = signal('');
  readonly enabledFilter = signal<ReadonlySet<boolean>>(new Set());
  readonly triggerFilter = signal<ReadonlySet<AutomationTriggerType>>(
    new Set()
  );

  private readonly search = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(250)),
    { initialValue: '' }
  );

  private readonly datatable = viewChild(
    DatatableComponent<AutomationRuleListItem>
  );

  readonly filtersActive = computed(() => {
    return (
      !!this.search().trim() ||
      this.enabledFilter().size > 0 ||
      this.triggerFilter().size > 0
    );
  });

  private readonly resourceParams = computed<Params>(() => {
    const search = this.search().trim();
    const enabled = [...this.enabledFilter()];
    const triggers = [...this.triggerFilter()];
    const isEnabled = enabled.length === 1 ? enabled[0] : null;

    return {
      ...(search ? { search } : {}),
      ...(isEnabled === null ? {} : { isEnabled }),
      ...(triggers.length ? { triggerTypes: triggers.join(',') } : {}),
    };
  });

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
      params: this.resourceParams,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, rule: AutomationRuleListItem) => rule.id,
    menu: this.canManage() ? this.rowMenu() : undefined,
    reloadSignal: this.reloadSignal(),
  }));

  constructor() {
    let previousSearch = this.search();

    effect(() => {
      const search = this.search();

      if (search === previousSearch) return;

      previousSearch = search;
      this.goToFirstPage();
    });
  }

  triggerLabel(trigger: AutomationTriggerType): string {
    return triggerTypeLabels[trigger];
  }

  toggleEnabledFilter(isEnabled: boolean) {
    this.enabledFilter.update((current) => toggle(current, isEnabled));
    this.goToFirstPage();
  }

  toggleTriggerFilter(triggerType: AutomationTriggerType) {
    this.triggerFilter.update((current) => toggle(current, triggerType));
    this.goToFirstPage();
  }

  private goToFirstPage() {
    this.datatable()?.goToPage(1);
  }

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
        icon: LucideSettings2,
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

function toggle<T>(current: ReadonlySet<T>, value: T): ReadonlySet<T> {
  const next = new Set(current);

  if (!next.delete(value)) {
    next.add(value);
  }

  return next;
}
