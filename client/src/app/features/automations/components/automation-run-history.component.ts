import {
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  output,
  signal,
  Signal,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { Page } from '@core/models/pagination';
import { fromNow } from '@core/util/dates';
import { debouncedSignal } from '@core/util/signals';
import { LucideChevronDown, LucideRefreshCw } from '@lucide/angular';
import { FilterInputComponent } from '@static/components/filter-input/filter-input.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { catchError, of, switchMap, tap } from 'rxjs';
import {
  actionTypeLabels,
  automationActionResultStatusLabels,
  countAutomationConditions,
  triggerTypeLabels,
} from '../models/automation-copy';
import { automationActionKeyword } from '../models/automation-flow-copy';
import {
  AutomationActionResult,
  AutomationActionResultStatus,
  AutomationRun,
  AutomationRunStatus,
  AutomationRunSummary,
  AutomationTrigger,
} from '../models/automation.models';
import { AutomationsService } from '../services/automations.service';
import { AutomationRunStatusPillComponent } from './automation-run-status-pill.component';

type RunStatusFilter = 'all' | 'succeeded' | 'skipped' | 'failed';

type RunStepTone = 'event' | 'success' | 'muted' | 'warn' | 'info';

interface RunStep {
  keyword: string;
  text: string;
  tone: RunStepTone;
}

const pageSize = 25;

const statusByFilter: Record<RunStatusFilter, AutomationRunStatus | null> = {
  all: null,
  succeeded: AutomationRunStatus.succeeded,
  skipped: AutomationRunStatus.skipped,
  failed: AutomationRunStatus.failed,
};

const stepDotClasses: Record<RunStepTone, string> = {
  event: 'bg-primary',
  success: 'bg-green-600 dark:bg-green-400',
  muted: 'bg-foreground/40',
  warn: 'bg-warn',
  info: 'bg-blue-600 dark:bg-blue-400',
};

const stepTextClasses: Record<RunStepTone, string> = {
  event: 'text-foreground',
  success: 'text-foreground',
  muted: 'text-foreground/60',
  warn: 'text-warn',
  info: 'text-foreground',
};

@Component({
  selector: 'app-automation-run-history',
  imports: [
    AutomationRunStatusPillComponent,
    FilterInputComponent,
    LucideChevronDown,
    LucideRefreshCw,
    PrettyDatePipe,
    SegmentedControlComponent,
    SpinnerComponent,
  ],
  host: { class: 'flex min-w-0 flex-col gap-3.5' },
  template: `
    <div class="flex flex-wrap items-center gap-3">
      <h2 class="mr-auto text-[17px] font-bold">
        <span i18n="Heading above the automation run history">
          Run history
        </span>
      </h2>

      <app-segmented-control
        variant="outlined"
        class="h-9.5 p-0.5"
        i18n-ariaLabel="Accessible label of the run outcome filter"
        ariaLabel="Filter runs by outcome"
        [options]="statusOptions()"
        [value]="statusFilter()"
        (valueChange)="setStatusFilter($event)" />

      <app-filter-input
        class="w-50 max-sm:flex-1"
        i18n-placeholder="Placeholder in the box that searches automation runs"
        placeholder="Search tasks"
        [(value)]="searchInput" />

      <button
        type="button"
        class="border-border text-foreground/60 hover:bg-foreground/3 focus-visible:ring-primary flex h-9.5 w-9.5 shrink-0 cursor-pointer items-center justify-center rounded-lg border transition-colors focus-visible:ring-2 focus-visible:outline-none"
        i18n-aria-label="Accessible label of the button that reloads the runs"
        aria-label="Refresh"
        i18n-title="Tooltip on the button that reloads the runs"
        title="Refresh"
        (click)="refresh.emit()">
        <svg lucideRefreshCw class="h-3.75 w-3.75"></svg>
      </button>
    </div>

    <div class="border-border bg-card overflow-hidden rounded-[10px] border">
      <div
        class="border-border text-foreground/50 grid h-10 grid-cols-[110px_minmax(0,1fr)_104px_20px] items-center gap-4 border-b px-4 text-xs font-semibold md:grid-cols-[150px_minmax(0,1.3fr)_minmax(0,1fr)_116px_28px]"
        aria-hidden="true">
        <span i18n="Column heading for when a run happened">When</span>
        <span i18n="Column heading for the task a run acted on">Task</span>
        <span class="max-md:hidden" i18n="Column heading for a run's result">
          Result
        </span>
        <span i18n="Column heading for a run's outcome">Status</span>
        <span></span>
      </div>

      @for (run of runs(); track run.id) {
        @let open = expandedId() === run.id;

        <div class="border-border/60 border-b">
          <button
            type="button"
            class="hover:bg-foreground/2 focus-visible:ring-primary grid min-h-14 w-full cursor-pointer grid-cols-[110px_minmax(0,1fr)_104px_20px] items-center gap-4 px-4 py-2 text-left transition-colors focus-visible:ring-2 focus-visible:outline-none focus-visible:ring-inset md:grid-cols-[150px_minmax(0,1.3fr)_minmax(0,1fr)_116px_28px]"
            [class.bg-foreground/2]="open"
            [attr.aria-expanded]="open"
            (click)="toggle(run.id)">
            <span class="flex flex-col gap-px">
              <span class="text-foreground text-sm">
                {{ relativeTime(run) }}
              </span>
              <span class="text-foreground/50 truncate text-xs">
                {{ run.createdAt | prettyDate }}
              </span>
            </span>

            <span class="flex min-w-0 items-baseline gap-2">
              @if (run.taskSystemId) {
                <span class="text-foreground/50 shrink-0 font-mono text-xs">
                  {{ run.taskSystemId }}
                </span>
              }
              <span class="text-foreground truncate text-sm">
                {{ taskLabel(run) }}
              </span>
            </span>

            <span
              class="text-foreground/60 min-w-0 text-[13px] leading-[1.4] text-pretty max-md:hidden">
              {{ resultLabel(run) }}
            </span>

            <span>
              <app-automation-run-status-pill [status]="run.status" />
            </span>

            <span class="text-foreground/45 flex justify-center">
              <svg
                lucideChevronDown
                class="h-4 w-4 transition-transform"
                [class.rotate-180]="open"></svg>
            </span>
          </button>

          @if (open) {
            <ol
              class="bg-foreground/2 flex flex-col px-4 pt-1 pb-4.5 md:pl-45.5"
              i18n-aria-label="Accessible name of the steps a run went through"
              aria-label="Run steps">
              @for (step of runSteps(run); track $index; let last = $last) {
                <li class="grid grid-cols-[20px_minmax(0,1fr)] gap-x-3">
                  <div class="relative flex justify-center pt-3.5">
                    @if (!last) {
                      <span
                        class="bg-border absolute top-7 -bottom-3.5 w-px"></span>
                    }
                    <span
                      class="relative h-2.5 w-2.5 rounded-full"
                      [class]="stepDotClass(step)"></span>
                  </div>
                  <div class="flex flex-col gap-0.5 pt-2.5 pb-1">
                    <span
                      class="text-foreground/50 text-[0.6875rem] font-bold tracking-[0.1em]">
                      {{ step.keyword }}
                    </span>
                    <span
                      class="text-sm leading-[1.45] text-pretty"
                      [class]="stepTextClass(step)">
                      {{ step.text }}
                    </span>
                  </div>
                </li>
              }
            </ol>
          }
        </div>
      } @empty {
        @if (loading()) {
          <div class="flex justify-center py-8">
            <app-spinner />
          </div>
        } @else if (failed()) {
          <p class="text-warn px-4 py-7 text-center text-sm">
            <span i18n="Shown when the automation run history fails to load">
              Automation runs could not be loaded.
            </span>
          </p>
        } @else {
          <p class="text-foreground/55 px-4 py-7 text-center text-sm">
            {{ emptyMessage() }}
          </p>
        }
      }

      <div
        class="text-foreground/55 flex h-12 items-center justify-between px-4 text-[13px]">
        <span
          i18n="
            How many runs are listed. SHOWN is the number on screen, TOTAL how
            many match
          ">
          Showing
          {{
            runs().length // i18n(ph="SHOWN")
          }}
          of
          {{
            totalCount() // i18n(ph="TOTAL")
          }}
          runs
        </span>

        @if (hasMore()) {
          <button
            type="button"
            class="text-primary hover:bg-foreground/5 focus-visible:ring-primary h-8 cursor-pointer rounded-md px-3 text-[13px] font-semibold transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-default disabled:opacity-60"
            [disabled]="loading()"
            (click)="loadMore()">
            <span i18n="Button that loads the next page of automation runs">
              Load more
            </span>
          </button>
        }
      </div>
    </div>
  `,
})
export class AutomationRunHistoryComponent {
  private readonly service = inject(AutomationsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly ruleId = input.required<number>();
  readonly trigger = input.required<AutomationTrigger>();
  readonly summary = input<AutomationRunSummary | null>(null);
  readonly reloadSignal = input<Signal<unknown>>(signal(0));

  readonly refresh = output();

  readonly statusFilter = signal<RunStatusFilter>('all');
  readonly searchInput = signal('');
  readonly expandedId = signal<number | null>(null);

  readonly runs = signal<AutomationRun[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly failed = signal(false);

  private readonly search = debouncedSignal(this.searchInput);
  private loadedPage = 0;

  readonly hasMore = computed(() => this.runs().length < this.totalCount());

  readonly statusOptions = computed<SegmentedOption<RunStatusFilter>[]>(() => {
    const summary = this.summary();

    return [
      {
        value: 'all',
        label: $localize`:Run history filter showing every run:All`,
        count: summary?.totalCount,
      },
      {
        value: 'succeeded',
        label: $localize`:Outcome status of an automation run:Succeeded`,
        count: summary?.succeededCount,
      },
      {
        value: 'skipped',
        label: $localize`:Outcome status of an automation run:Skipped`,
        count: summary?.skippedCount,
      },
      {
        value: 'failed',
        label: $localize`:Outcome status of an automation run:Failed`,
        count: summary?.failedCount,
      },
    ];
  });

  readonly emptyMessage = computed(() => {
    const filtered = this.statusFilter() !== 'all' || !!this.search().trim();

    return filtered
      ? $localize`:Shown when no automation runs match the active filters:No runs match these filters`
      : $localize`:Empty state for the run history:No automation runs recorded yet`;
  });

  private readonly query = computed(() => {
    return {
      ruleId: this.ruleId(),
      status: statusByFilter[this.statusFilter()],
      search: this.search().trim(),
      reload: this.reloadSignal()(),
    };
  });

  private readonly hasConditions = computed(() => {
    return countAutomationConditions(this.trigger().conditionGroup) > 0;
  });

  constructor() {
    toObservable(this.query)
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.failed.set(false);
        }),
        switchMap((query) => this.fetchPage(query, 1)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((page) => {
        this.loadedPage = page ? 1 : 0;
        this.runs.set(page?.items ?? []);
        this.totalCount.set(page?.totalCount ?? 0);
        this.loading.set(false);
      });
  }

  setStatusFilter(filter: RunStatusFilter) {
    this.statusFilter.set(filter);
  }

  loadMore() {
    const nextPage = this.loadedPage + 1;

    this.loading.set(true);

    this.fetchPage(this.query(), nextPage)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((page) => {
        if (page) {
          this.loadedPage = nextPage;
          this.runs.update((runs) => [...runs, ...page.items]);
          this.totalCount.set(page.totalCount);
        }

        this.loading.set(false);
      });
  }

  toggle(runId: number) {
    this.expandedId.update((current) => (current === runId ? null : runId));
  }

  relativeTime(run: AutomationRun): string {
    return fromNow(run.createdAt);
  }

  taskLabel(run: AutomationRun): string {
    if (run.taskName) return run.taskName;

    return $localize`:Shown for a run whose task no longer exists:Deleted task`;
  }

  resultLabel(run: AutomationRun): string {
    if (run.status === AutomationRunStatus.failed) {
      const failedAction = run.actionResults.find(
        (result) => result.status === AutomationActionResultStatus.failed
      );

      return (
        failedAction?.message ??
        run.message ??
        $localize`:Result of an automation run that failed without a message:An action failed`
      );
    }

    if (run.status === AutomationRunStatus.skipped) {
      return (
        run.message ??
        $localize`:Result of an automation run whose conditions did not match:Conditions not met`
      );
    }

    if (run.message) return run.message;

    const outcomes = run.actionResults.map((result) => {
      return this.actionOutcome(result);
    });

    return outcomes.length
      ? outcomes.join(', ')
      : $localize`:Result of an automation run that completed:Completed`;
  }

  runSteps(run: AutomationRun): RunStep[] {
    const steps: RunStep[] = [
      {
        keyword: $localize`:Heading of the trigger part of the rule:WHEN`,
        text: triggerTypeLabels[run.triggerType],
        tone: 'event',
      },
    ];

    const stoppedAtConditions =
      run.status === AutomationRunStatus.skipped && !run.actionResults.length;

    steps.push(this.conditionStep(run, stoppedAtConditions));

    run.actionResults.forEach((result, index) => {
      steps.push(this.actionStep(result, index));
    });

    const failedBeforeActions =
      run.status === AutomationRunStatus.failed && !run.actionResults.length;

    if (failedBeforeActions) {
      steps.push({
        keyword: $localize`:Heading of the step where an automation run failed:FAILED`,
        text:
          run.message ??
          $localize`:Result of an automation run that failed without a message:An action failed`,
        tone: 'warn',
      });
    }

    return steps;
  }

  stepDotClass(step: RunStep): string {
    return stepDotClasses[step.tone];
  }

  stepTextClass(step: RunStep): string {
    return stepTextClasses[step.tone];
  }

  private conditionStep(run: AutomationRun, stopped: boolean): RunStep {
    const keyword = $localize`:Heading of the conditions part of the rule:IF`;

    if (stopped) {
      return {
        keyword,
        text:
          run.message ??
          $localize`:Run step when the conditions did not match:Conditions not matched, so the run stopped here`,
        tone: 'muted',
      };
    }

    if (!this.hasConditions()) {
      return {
        keyword,
        text: $localize`:Run step when the rule has no conditions:No conditions, so the task continued`,
        tone: 'success',
      };
    }

    return {
      keyword,
      text: $localize`:Run step when the conditions matched:Conditions matched`,
      tone: 'success',
    };
  }

  private actionStep(result: AutomationActionResult, index: number): RunStep {
    const outcome = this.actionOutcome(result);
    const text = result.message ? `${outcome}: ${result.message}` : outcome;

    return {
      keyword: automationActionKeyword(index),
      text,
      tone: this.actionTone(result.status),
    };
  }

  private actionOutcome(result: AutomationActionResult): string {
    const action = actionTypeLabels[result.actionType];
    const status = automationActionResultStatusLabels[result.status];

    return $localize`:Outcome of one automation action in a run. ACTION is the action name, STATUS its outcome:${action}:ACTION: — ${status}:STATUS:`;
  }

  private actionTone(status: AutomationActionResultStatus): RunStepTone {
    switch (status) {
      case AutomationActionResultStatus.succeeded:
        return 'success';
      case AutomationActionResultStatus.failed:
        return 'warn';
      case AutomationActionResultStatus.scheduled:
      case AutomationActionResultStatus.pending:
        return 'info';
      case AutomationActionResultStatus.skipped:
        return 'muted';
    }
  }

  private fetchPage(
    query: {
      ruleId: number;
      status: AutomationRunStatus | null;
      search: string;
    },
    page: number
  ) {
    return this.service
      .getRuns(
        query.ruleId,
        { page, pageSize },
        { status: query.status, search: query.search || null }
      )
      .pipe(
        catchError(() => {
          this.failed.set(true);

          return of<Page<AutomationRun> | null>(null);
        })
      );
  }
}
