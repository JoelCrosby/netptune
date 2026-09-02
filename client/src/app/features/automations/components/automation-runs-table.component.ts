import {
  Component,
  computed,
  effect,
  input,
  signal,
  Signal,
  viewChild,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Params } from '@angular/router';
import { debounceTime } from 'rxjs/operators';
import {
  actionTypeLabels,
  automationActionResultStatusLabels,
  automationRunStatuses,
  automationRunStatusLabels,
  automationTriggerTypes,
  entityTargetLabel,
  runStatusClass,
  triggerTypeLabels,
} from '../models/automation-copy';
import {
  AutomationActionResult,
  AutomationActionResultStatus,
  AutomationRun,
  AutomationRunStatus,
  AutomationTriggerType,
} from '../models/automation.models';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import {
  DatatableDataSource,
  DatatableSort,
} from '@static/components/datatable/datatable.types';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuCheckboxItemComponent } from '@static/components/dropdown-menu/menu-checkbox-item.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { FilterActionButtonComponent } from '@static/components/filter-action-button/filter-action-button.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import { LucideCircleDashed, LucideZap } from '@lucide/angular';

@Component({
  selector: 'app-automation-runs-table',
  imports: [
    PrettyDatePipe,
    DatatableComponent,
    DatatableCellTemplateDirective,
    DatatableEmptyDirective,
    DropdownMenuComponent,
    EmptyStateComponent,
    FilterActionButtonComponent,
    MenuCheckboxItemComponent,
    SearchInputComponent,
  ],
  template: `
    <div class="mb-3 flex flex-row flex-wrap items-center gap-2">
      <app-search-input
        [term]="searchInput()"
        (searchChange)="searchInput.set($event ?? '')" />

      <div #statusAnchor>
        <app-filter-action-button
          i18n-label="Label on the control that filters runs by outcome"
          label="Filter by Status"
          [icon]="lucideCircleDashed"
          [color]="statusFilter().size ? 'primary' : undefined"
          [count]="statusFilter().size"
          (action)="statusMenu.toggle(statusAnchor)" />
      </div>

      <app-dropdown-menu #statusMenu>
        @for (status of runStatuses; track status) {
          <button
            app-menu-checkbox-item
            [checked]="statusFilter().has(status)"
            (checkedChange)="toggleStatusFilter(status)">
            {{ statusLabel(status) }}
          </button>
        }
      </app-dropdown-menu>

      <div #triggerAnchor>
        <app-filter-action-button
          i18n-label="Label on the control that filters runs by trigger"
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
      i18n-errorMessage="Shown when the automation run history fails to load"
      errorMessage="Automation runs could not be loaded."
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

      <ng-template appDatatableEmpty>
        <app-empty-state
          compact
          [title]="emptyTitle()"
          [description]="emptyDescription()" />
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

  readonly runStatuses = automationRunStatuses;
  readonly triggerTypes = automationTriggerTypes;

  readonly lucideCircleDashed = LucideCircleDashed;
  readonly lucideZap = LucideZap;

  readonly searchInput = signal('');
  readonly statusFilter = signal<ReadonlySet<AutomationRunStatus>>(new Set());
  readonly triggerFilter = signal<ReadonlySet<AutomationTriggerType>>(
    new Set()
  );

  private readonly search = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(250)),
    { initialValue: '' }
  );

  private readonly datatable = viewChild(DatatableComponent<AutomationRun>);

  readonly filtersActive = computed(() => {
    return (
      !!this.search().trim() ||
      this.statusFilter().size > 0 ||
      this.triggerFilter().size > 0
    );
  });

  readonly emptyTitle = computed(() => {
    if (this.filtersActive()) {
      return $localize`:Shown when no automation runs match the active filters:No runs match these filters`;
    }

    return $localize`:Empty state for the run history:No automation runs recorded yet`;
  });

  readonly emptyDescription = computed(() => {
    if (!this.filtersActive()) return '';

    return $localize`:Advice shown when filters exclude every row:Try a different search or filter.`;
  });

  private readonly resourceParams = computed<Params>(() => {
    const search = this.search().trim();
    const statuses = [...this.statusFilter()];
    const triggers = [...this.triggerFilter()];

    return {
      ...(search ? { search } : {}),
      ...(statuses.length ? { statuses: statuses.join(',') } : {}),
      ...(triggers.length ? { triggerTypes: triggers.join(',') } : {}),
    };
  });

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

  constructor() {
    let previousSearch = this.search();

    effect(() => {
      const search = this.search();

      if (search === previousSearch) return;

      previousSearch = search;
      this.goToFirstPage();
    });
  }

  toggleStatusFilter(status: AutomationRunStatus) {
    this.statusFilter.update((current) => toggle(current, status));
    this.goToFirstPage();
  }

  toggleTriggerFilter(triggerType: AutomationTriggerType) {
    this.triggerFilter.update((current) => toggle(current, triggerType));
    this.goToFirstPage();
  }

  private goToFirstPage() {
    this.datatable()?.goToPage(1);
  }

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

  triggerLabel(triggerType: AutomationTriggerType): string {
    return triggerTypeLabels[triggerType];
  }

  statusLabel(status: AutomationRunStatus): string {
    return automationRunStatusLabels[status];
  }

  targetLabel(run: AutomationRun): string {
    return entityTargetLabel(run.entityType, run.entityId);
  }
}

function toggle<T>(current: ReadonlySet<T>, value: T): ReadonlySet<T> {
  const next = new Set(current);

  if (!next.delete(value)) {
    next.add(value);
  }

  return next;
}
