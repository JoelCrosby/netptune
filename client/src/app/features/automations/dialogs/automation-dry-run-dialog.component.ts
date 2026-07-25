import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import {
  takeUntilDestroyed,
  toObservable,
  toSignal,
} from '@angular/core/rxjs-interop';
import { Params } from '@angular/router';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { sprintResource } from '@core/resources/sprint.resource';
import { statusResource } from '@core/resources/status.resources';
import { userResource } from '@core/resources/user.resource';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { finalize } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { AutomationConditionExplanationComponent } from '../components/automation-condition-explanation.component';
import { AutomationDryRunEffectsComponent } from '../components/automation-dry-run-effects.component';
import { triggerTypeLabels } from '../models/automation-copy';
import { AutomationDryRun } from '../models/automation.models';
import { AutomationsService } from '../services/automations.service';

export interface AutomationDryRunDialogData {
  ruleId: number;
  ruleName: string;
}

@Component({
  selector: 'app-automation-dry-run-dialog',
  imports: [
    AutomationConditionExplanationComponent,
    AutomationDryRunEffectsComponent,
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    DatatableComponent,
    DatatableCellTemplateDirective,
    FormInputComponent,
    TaskScopeIdComponent,
  ],
  template: `
    <app-dialog-title>Test Automation</app-dialog-title>

    <div class="flex w-220 max-w-full flex-col gap-4">
      <p class="text-muted text-sm">
        Check whether {{ dialogData.ruleName }} would run against a task.
        Testing changes nothing — use Run now to apply the actions.
      </p>

      <app-form-input
        name="dry-run-search"
        placeholder="Search tasks by name, key or tag"
        [noMargin]="true"
        [value]="searchInput()"
        (valueChange)="searchInput.set($event)" />

      <app-datatable
        containerClass="h-[320px] overflow-y-auto overflow-x-hidden"
        tableClass="table-fixed"
        rowClass="bg-card"
        emptyMessage="No tasks available to test."
        [data]="data"
        [stickyHeader]="true">
        <ng-template appDatatableCell="systemId" let-task>
          <app-task-scope-id [id]="task.systemId" />
        </ng-template>

        <ng-template appDatatableCell="name" let-task>
          <span class="block truncate font-medium">{{ task.name }}</span>
        </ng-template>

        <ng-template appDatatableCell="action" let-task>
          <button
            app-stroked-button
            type="button"
            [disabled]="running()"
            (click)="onTest(task)">
            Test
          </button>
        </ng-template>
      </app-datatable>

      @if (failed()) {
        <p class="text-warn text-sm">Could not test this task.</p>
      } @else if (dryRun(); as dryRun) {
        <div class="border-border flex flex-col gap-3 rounded-md border p-3">
          <div class="flex flex-col gap-1">
            @if (triggerBlocks(dryRun)) {
              <p class="text-sm font-medium">
                This rule would not run against
                <span class="text-primary">{{ dryRun.taskName }}</span>
              </p>
              <p class="text-muted text-xs">
                "{{ triggerLabel(dryRun) }}" does not apply to this task right
                now, so the rule would never reach its conditions.
              </p>
            } @else if (dryRun.conditionsMatch) {
              <p class="text-sm font-medium">
                This rule would run against
                <span class="text-primary">{{ dryRun.taskName }}</span>
              </p>
            } @else {
              <p class="text-sm font-medium">
                This rule would not run against
                <span class="text-primary">{{ dryRun.taskName }}</span>
              </p>
            }

            @if (!dryRun.triggerIsEvaluable) {
              <p class="text-muted text-xs">
                "{{ triggerLabel(dryRun) }}" only fires while a task is
                changing, so the conditions above are checked against the task
                as it stands now.
              </p>
            }

            @if (!dryRun.isEnabled) {
              <p class="text-warn text-xs">
                The rule is disabled, so it will not run until you enable it.
              </p>
            }

            @if (dryRun.hasUnevaluableConditions) {
              <p class="text-muted text-xs">
                Some conditions only apply while a task is changing, so they
                cannot be checked here.
              </p>
            }
          </div>

          @if (dryRun.conditionGroup; as conditionGroup) {
            <app-automation-condition-explanation
              [group]="conditionGroup"
              [statuses]="statuses()"
              [sprints]="sprints()"
              [users]="users()" />
          } @else {
            <p class="text-muted text-sm">
              This rule has no conditions, so every triggering task matches.
            </p>
          }

          @if (dryRun.actions.length) {
            <app-automation-dry-run-effects
              [actions]="dryRun.actions"
              [users]="users()" />
          }

          <div class="flex items-center gap-3">
            <button
              app-flat-button
              type="button"
              [disabled]="running() || queueing()"
              (click)="onRunNow(dryRun)">
              Run now
            </button>
            <span class="text-muted text-xs">
              Runs the actions above against {{ dryRun.taskName }}.
            </span>
          </div>

          @if (queued()) {
            <p class="text-xs">
              Queued. The run appears in this rule's history once it completes.
            </p>
          }

          @if (queueFailed()) {
            <p class="text-warn text-xs">Could not start this run.</p>
          }
        </div>
      }
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="close()">Close</button>
    </div>
  `,
})
export class AutomationDryRunDialogComponent {
  private readonly dialogRef =
    inject<DialogRef<void, AutomationDryRunDialogComponent>>(DialogRef);
  private readonly service = inject(AutomationsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly dialogData = inject<AutomationDryRunDialogData>(DIALOG_DATA);

  private readonly statusesResource = statusResource();
  private readonly sprintsResource = sprintResource([]);
  private readonly usersResource = userResource();

  readonly statuses = this.statusesResource.value;
  readonly sprints = this.sprintsResource.value;

  readonly users = computed(() => {
    return this.usersResource.value()?.payload?.items ?? [];
  });

  readonly searchInput = signal('');
  readonly running = signal(false);
  readonly failed = signal(false);
  readonly queueing = signal(false);
  readonly queued = signal(false);
  readonly queueFailed = signal(false);
  readonly dryRun = signal<AutomationDryRun | null>(null);

  private search = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(250)),
    { initialValue: '' }
  );

  private params = computed<Params>(() => {
    const search = this.search().trim();

    return search ? { search } : {};
  });

  readonly data: DatatableDataSource<TaskViewModel> = {
    key: 'automation-dry-run-tasks',
    columns: [
      { id: 'systemId', header: 'Key', sortable: true, widthClass: 'w-28' },
      {
        id: 'name',
        header: 'Task',
        accessor: 'name',
        sortable: true,
        cellClass: 'min-w-0',
      },
      { id: 'action', header: '', widthClass: 'w-24', align: 'end' },
    ],
    resource: {
      url: 'api/tasks',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, task: TaskViewModel) => task.id,
  };

  triggerBlocks(dryRun: AutomationDryRun): boolean {
    return dryRun.triggerIsEvaluable && !dryRun.triggerMatches;
  }

  triggerLabel(dryRun: AutomationDryRun): string {
    return triggerTypeLabels[dryRun.triggerType];
  }

  onRunNow(dryRun: AutomationDryRun) {
    this.queued.set(false);
    this.queueFailed.set(false);
    this.queueing.set(true);

    this.service
      .runNow(this.dialogData.ruleId, [dryRun.taskId])
      .pipe(
        finalize(() => this.queueing.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => this.queued.set(true),
        error: () => this.queueFailed.set(true),
      });
  }

  onTest(task: TaskViewModel) {
    this.failed.set(false);
    this.queued.set(false);
    this.queueFailed.set(false);
    this.running.set(true);

    this.service
      .dryRun(this.dialogData.ruleId, task.id)
      .pipe(
        finalize(() => this.running.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (dryRun) => this.dryRun.set(dryRun),
        error: () => {
          this.dryRun.set(null);
          this.failed.set(true);
        },
      });
  }

  close() {
    this.dialogRef.close();
  }
}
