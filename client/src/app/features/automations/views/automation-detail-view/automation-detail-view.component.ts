import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { Status } from '@core/models/status';
import { workspaceBoardsResource } from '@core/resources/board.resource';
import { projectResource } from '@core/resources/project.resource';
import { serviceAccountResource } from '@core/resources/service-account.resource';
import { sprintResource } from '@core/resources/sprint.resource';
import { ConfirmationService } from '@core/services/confirmation.service';
import { DialogService } from '@core/services/dialog.service';
import { StatusesService } from '@core/services/statuses.service';
import { mutation } from '@core/util/mutation';
import { reloadToken } from '@core/util/signals';
import {
  LucideCopy,
  LucideEllipsis,
  LucideFlaskConical,
  LucidePencil,
  LucideTrash2,
  LucideTriangleAlert,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { MenuSeparatorComponent } from '@static/components/dropdown-menu/menu-separator.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { finalize, firstValueFrom, forkJoin } from 'rxjs';
import { AutomationRuleRailComponent } from '../../components/automation-rule-rail.component';
import { AutomationRunHistoryComponent } from '../../components/automation-run-history.component';
import { AutomationStatePillComponent } from '../../components/automation-state-pill.component';
import {
  AutomationCloneDialogComponent,
  AutomationCloneDialogData,
  AutomationCloneDialogResult,
} from '../../dialogs/automation-clone-dialog.component';
import {
  AutomationDryRunDialogComponent,
  AutomationDryRunDialogData,
} from '../../dialogs/automation-dry-run-dialog.component';
import {
  describeAutomationRunsAs,
  describeServiceAccountName,
  resolveAutomationScope,
} from '../../models/automation-flow-copy';
import {
  AutomationRule,
  AutomationRunSummary,
} from '../../models/automation.models';
import { AutomationsService } from '../../services/automations.service';

@Component({
  selector: 'app-automation-detail-view',
  imports: [
    AutomationRuleRailComponent,
    AutomationRunHistoryComponent,
    AutomationStatePillComponent,
    DropdownMenuComponent,
    ErrorStateComponent,
    FlatButtonComponent,
    LucideCopy,
    LucideEllipsis,
    LucideFlaskConical,
    LucidePencil,
    LucideTrash2,
    LucideTriangleAlert,
    MenuItemComponent,
    MenuSeparatorComponent,
    PageContainerComponent,
    PageLoadingComponent,
    PrettyDatePipe,
    RouterLink,
    StrokedButtonComponent,
  ],
  template: `
    <app-page-container
      followsWidthPreference
      [centerPage]="true"
      [marginBottom]="true">
      @if (loading()) {
        <app-page-loading />
      } @else if (error()) {
        <app-error-state
          i18n-title="Shown when a single automation fails to load"
          title="Automation could not be loaded"
          i18n-description="Advice shown when a page fails to load"
          description="Check your connection and try again."
          (retry)="load()" />
      } @else if (rule(); as rule) {
        <div class="mx-auto flex w-full max-w-310 flex-col gap-7 pt-4">
          <header class="flex flex-wrap items-start gap-5">
            <div class="flex min-w-0 flex-1 flex-col gap-1.5">
              <div class="flex flex-wrap items-center gap-3">
                <h1 class="text-[26px] font-bold tracking-[-0.4px]">
                  {{ rule.name }}
                </h1>
                @if (rule.isEnabled) {
                  <app-automation-state-pill tone="success">
                    <span i18n="Marks an automation that is switched on">
                      Enabled
                    </span>
                  </app-automation-state-pill>
                } @else {
                  <app-automation-state-pill>
                    <span i18n="Marks an automation that is switched off">
                      Paused
                    </span>
                  </app-automation-state-pill>
                }
              </div>

              <p class="text-foreground/55 text-sm leading-normal">
                {{ runsAs() }}
                @if (rule.updatedAt) {
                  <span
                    i18n="
                      When an automation was last changed, shown after the
                      creation date. Keep the leading separator. DATE is a
                      formatted date
                    ">
                    · Updated
                    {{
                      rule.updatedAt | prettyDate // i18n(ph="DATE")
                    }}
                  </span>
                }
              </p>
            </div>

            <div class="flex shrink-0 flex-wrap items-center gap-2.5">
              <button
                app-stroked-button
                class="gap-2"
                type="button"
                (click)="onDryRun(rule)">
                <svg lucideFlaskConical class="h-3.75 w-3.75"></svg>
                <span i18n="Button that tests the automation against a task">
                  Test run
                </span>
              </button>

              @if (canManage()) {
                <button
                  app-stroked-button
                  type="button"
                  [disabled]="saving.pending()"
                  (click)="onToggle(rule)">
                  @if (rule.isEnabled) {
                    <span i18n="Button that pauses an automation">Pause</span>
                  } @else {
                    <span i18n="Button that resumes a paused automation">
                      Resume
                    </span>
                  }
                </button>

                <a
                  app-flat-button
                  color="primary"
                  class="gap-2"
                  [routerLink]="['edit']">
                  <svg lucidePencil class="h-3.75 w-3.75"></svg>
                  <span i18n="Button that edits the automation">Edit</span>
                </a>

                <div #moreAnchor>
                  <button
                    app-stroked-button
                    class="w-9 px-0"
                    type="button"
                    i18n-aria-label="
                      Accessible label of the button that opens more automation
                      actions
                    "
                    aria-label="More actions"
                    [disabled]="saving.pending()"
                    (click)="moreMenu.toggle(moreAnchor)">
                    <svg lucideEllipsis class="h-4 w-4"></svg>
                  </button>
                </div>

                <app-dropdown-menu #moreMenu panelClass="w-50">
                  <button
                    app-menu-item
                    (click)="moreMenu.close(); onClone(rule)">
                    <svg lucideCopy class="h-3.75 w-3.75"></svg>
                    <span i18n="Menu item that duplicates an automation">
                      Duplicate
                    </span>
                  </button>
                  <app-menu-separator />
                  <button
                    app-menu-item
                    color="warn"
                    (click)="moreMenu.close(); onDelete(rule)">
                    <svg lucideTrash2 class="h-3.75 w-3.75"></svg>
                    <span i18n="Menu item that deletes an automation">
                      Delete automation
                    </span>
                  </button>
                </app-dropdown-menu>
              }
            </div>
          </header>

          @if (rule.autoDisabledReason) {
            <section
              class="border-warn/40 bg-warn/5 flex flex-col gap-2 rounded-lg border p-4"
              role="alert">
              <h2 class="flex items-center gap-2 text-sm font-semibold">
                <svg lucideTriangleAlert class="text-warn h-4 w-4"></svg>
                <span i18n="Heading of the auto-disabled warning">
                  This automation was disabled automatically
                </span>
              </h2>
              <p class="text-sm">{{ rule.autoDisabledReason }}</p>
              <p class="text-foreground/60 text-sm">
                <span i18n="Advice on the auto-disabled warning">
                  Fix the underlying problem before enabling it again, or it
                  will be disabled once more.
                </span>
              </p>
            </section>
          }

          @if (rule.warnings.length) {
            <section
              class="border-warn/40 bg-warn/5 flex flex-col gap-2 rounded-lg border p-4"
              role="alert">
              <h2 class="flex items-center gap-2 text-sm font-semibold">
                <svg lucideTriangleAlert class="text-warn h-4 w-4"></svg>
                <span i18n="Heading of the broken-reference warning">
                  This automation references items that no longer exist
                </span>
              </h2>
              <ul class="ml-6 list-disc text-sm">
                @for (warning of rule.warnings; track $index) {
                  <li>{{ warning.message }}</li>
                }
              </ul>
              <p class="text-foreground/60 text-sm">
                <span i18n="Advice on the broken-reference warning">
                  Edit the automation to point these at something that still
                  exists, otherwise its runs will fail.
                </span>
              </p>
            </section>
          }

          <div
            class="grid items-start gap-7 lg:grid-cols-[minmax(0,1fr)_320px]">
            <app-automation-run-history
              [ruleId]="rule.id"
              [trigger]="rule.trigger"
              [summary]="runSummary()"
              [reloadSignal]="runsReload"
              (refresh)="refreshRuns()" />

            <aside class="flex flex-col gap-4 lg:sticky lg:top-4">
              <app-automation-rule-rail
                [trigger]="rule.trigger"
                [actions]="rule.actions"
                [statuses]="statuses()"
                [editLink]="canManage() ? ['edit'] : null" />

              <dl
                class="border-border bg-border grid grid-cols-2 gap-px overflow-hidden rounded-xl border">
                <div class="bg-card px-4 py-3.5">
                  <dt class="text-foreground/55 mb-0.5 text-xs font-medium">
                    <span i18n="Stat label for how many times a rule has run">
                      Runs
                    </span>
                  </dt>
                  <dd class="text-[22px] font-bold">{{ totalRuns() }}</dd>
                </div>
                <div class="bg-card px-4 py-3.5">
                  <dt class="text-foreground/55 mb-0.5 text-xs font-medium">
                    <span
                      i18n="Stat label for the share of runs that succeeded">
                      Succeeded
                    </span>
                  </dt>
                  <dd class="text-[22px] font-bold">{{ successRate() }}</dd>
                </div>
                <div class="bg-card col-span-2 px-4 py-3.5">
                  <dt class="text-foreground/55 mb-0.5 text-xs font-medium">
                    <span i18n="Stat label for when a rule last ran">
                      Last run
                    </span>
                  </dt>
                  <dd class="text-[15px] font-semibold">
                    @if (runSummary()?.lastRunAt; as lastRunAt) {
                      {{ lastRunAt | prettyDate }}
                    } @else {
                      <span i18n="Shown when an automation has never run">
                        Not run yet
                      </span>
                    }
                  </dd>
                </div>
              </dl>

              <p class="text-foreground/50 mx-1 text-[13px] leading-normal">
                <span
                  i18n="
                    When an automation was created. DATE is a formatted date
                  ">
                  Created
                  {{
                    rule.createdAt | prettyDate // i18n(ph="DATE")
                  }}
                </span>
              </p>
            </aside>
          </div>
        </div>
      }
    </app-page-container>
  `,
})
export class AutomationDetailViewComponent {
  private service = inject(AutomationsService);
  private statusesService = inject(StatusesService);
  private confirmation = inject(ConfirmationService);
  private dialog = inject(DialogService);
  private snackbar = inject(SnackbarService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  readonly rule = signal<AutomationRule | null>(null);
  readonly runSummary = signal<AutomationRunSummary | null>(null);
  readonly statuses = signal<Status[]>([]);
  readonly runsReload = reloadToken();
  readonly loading = signal(true);
  readonly saving = mutation();
  readonly error = signal(false);
  readonly canManage = hasPermission(PERMISSIONS.automations.manage);

  readonly serviceAccountsResource = serviceAccountResource();
  readonly projectsResource = projectResource();
  readonly workspaceBoardsResource = workspaceBoardsResource();
  readonly workspaceSprintsResource = sprintResource([]);

  readonly runsAs = computed(() => {
    const rule = this.rule();

    if (!rule) return '';

    const account = describeServiceAccountName(
      this.serviceAccountsResource.value(),
      rule.executionUserId
    );
    const scope = resolveAutomationScope(rule, {
      projects: this.projectsResource.value(),
      boards: this.workspaceBoardsResource.value(),
      sprints: this.workspaceSprintsResource.value(),
    });

    return describeAutomationRunsAs(account, scope);
  });

  readonly totalRuns = computed(() => this.runSummary()?.totalCount ?? 0);

  readonly successRate = computed(() => {
    const summary = this.runSummary();

    if (!summary?.totalCount) return '—';

    const rate = Math.round(
      (summary.succeededCount / summary.totalCount) * 100
    );

    return `${rate}%`;
  });

  constructor() {
    this.load();
  }

  load() {
    const id = this.ruleId();
    if (!id) {
      this.error.set(true);
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(false);

    forkJoin({
      rule: this.service.getRule(id),
      runSummary: this.service.getRunSummary(id),
      statuses: this.statusesService.get(),
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: ({ rule, runSummary, statuses }) => {
          this.rule.set(rule);
          this.runSummary.set(runSummary);
          this.statuses.set(statuses);
        },
        error: () => this.error.set(true),
      });
  }

  refreshRuns() {
    const id = this.ruleId();

    if (!id) return;

    this.runsReload.bump();

    this.service
      .getRunSummary(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((summary) => this.runSummary.set(summary));
  }

  onDryRun(rule: AutomationRule) {
    const data: AutomationDryRunDialogData = {
      ruleId: rule.id,
      ruleName: rule.name,
    };

    this.dialog.open(AutomationDryRunDialogComponent, { data });
  }

  onToggle(rule: AutomationRule) {
    const request = rule.isEnabled
      ? this.service.disable(rule.id)
      : this.service.enable(rule.id);

    this.saving.run(request.pipe(takeUntilDestroyed(this.destroyRef)), {
      onSuccess: () => {
        this.snackbar.open(
          rule.isEnabled
            ? $localize`:Confirmation after pausing an automation:Automation paused`
            : $localize`:Confirmation after resuming an automation:Automation resumed`
        );
        this.load();
      },
      onError: () => {
        this.snackbar.error(
          $localize`:Error after failing to update an automation:Automation could not be updated`
        );
      },
    });
  }

  async onClone(rule: AutomationRule) {
    const data: AutomationCloneDialogData = {
      ruleName: rule.name,
      trigger: rule.trigger,
      actions: rule.actions,
      statuses: this.statuses(),
    };

    const result = await this.dialog.openForResult<
      AutomationCloneDialogResult,
      AutomationCloneDialogData
    >(AutomationCloneDialogComponent, { data });

    if (!result) return;

    this.saving.run(
      this.service
        .clone(rule.id, result.name)
        .pipe(takeUntilDestroyed(this.destroyRef)),
      {
        onSuccess: (clone) => {
          this.snackbar.open(`Created "${clone.name}" as a disabled copy`);
          void this.router.navigate(['../', clone.id, 'edit'], {
            relativeTo: this.route,
          });
        },
        onError: () => {
          this.snackbar.error(
            $localize`:Error after failing to clone an automation:Automation could not be cloned`
          );
        },
      }
    );
  }

  async onDelete(rule: AutomationRule) {
    const confirmed = await firstValueFrom(
      this.confirmation.open({
        title: $localize`:Title of the confirmation dialog for deleting an automation:Delete Automation`,
        message: `Delete "${rule.name}"? This cannot be undone.`,
        acceptLabel: $localize`:Confirms a destructive action:Delete`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      }),
      { defaultValue: false }
    );

    if (!confirmed) return;

    this.saving.run(
      this.service.delete(rule.id).pipe(takeUntilDestroyed(this.destroyRef)),
      {
        onSuccess: () => {
          this.snackbar.open(
            $localize`:Confirmation after deleting an automation:Automation deleted`
          );
          void this.router.navigate(['../'], { relativeTo: this.route });
        },
        onError: () => {
          this.snackbar.error(
            $localize`:Error after failing to delete an automation:Automation could not be deleted`
          );
        },
      }
    );
  }

  private ruleId(): number | null {
    const value = Number(this.route.snapshot.paramMap.get('id'));
    return Number.isFinite(value) && value > 0 ? value : null;
  }
}
