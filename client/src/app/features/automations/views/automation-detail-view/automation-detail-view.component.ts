import { Component, DestroyRef, inject, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PERMISSONS } from '@core/auth/permissions';
import { ConfirmationService } from '@core/services/confirmation.service';
import { DialogService } from '@core/services/dialog.service';
import { StatusesService } from '@core/services/statuses.service';
import { Status } from '@core/models/status';
import {
  LucideCirclePause,
  LucideCirclePlay,
  LucideFlaskConical,
  LucideSettings2,
  LucideTriangleAlert,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { EMPTY, finalize, forkJoin, switchMap } from 'rxjs';
import { AutomationDetailHeadingComponent } from '../../components/automation-detail-heading.component';
import { AutomationDetailStatsComponent } from '../../components/automation-detail-stats.component';
import {
  AutomationDryRunDialogComponent,
  AutomationDryRunDialogData,
} from '../../dialogs/automation-dry-run-dialog.component';
import { AutomationRunsTableComponent } from '../../components/automation-runs-table.component';
import { AutomationRuleSummaryComponent } from '../../components/automation-rule-summary.component';
import { AutomationRule, AutomationRun } from '../../models/automation.models';
import { AutomationsService } from '../../services/automations.service';

@Component({
  selector: 'app-automation-detail-view',
  imports: [
    ErrorStateComponent,
    RouterLink,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    AutomationDetailHeadingComponent,
    AutomationDetailStatsComponent,
    AutomationRunsTableComponent,
    AutomationRuleSummaryComponent,
    LucideSettings2,
    LucideCirclePause,
    LucideCirclePlay,
    LucideFlaskConical,
    LucideTriangleAlert,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for a single automation"
        title="Automation">
        <a
          app-stroked-button
          [routerLink]="['../']"
          i18n="Link back to the automation list">
          Back
        </a>
        @if (rule(); as rule) {
          <button app-stroked-button type="button" (click)="onDryRun(rule)">
            <svg lucideFlaskConical class="h-4 w-4"></svg>
            <span i18n="Button that tests the automation against a task">
              Test
            </span>
          </button>
        }
        @if (canManage() && rule(); as rule) {
          <button
            app-stroked-button
            type="button"
            [disabled]="saving()"
            (click)="onToggle(rule)">
            @if (rule.isEnabled) {
              <svg lucideCirclePause class="h-4 w-4"></svg>
              <span i18n="Button that switches an automation off">Disable</span>
            } @else {
              <svg lucideCirclePlay class="h-4 w-4"></svg>
              <span i18n="Button that switches an automation on">Enable</span>
            }
          </button>
          <a app-flat-button color="primary" [routerLink]="['edit']">
            <svg lucideSettings2 class="h-4 w-4"></svg>
            <span i18n="Button that edits the automation">Edit</span>
          </a>
        }
      </app-page-header>

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
        <section class="flex flex-col gap-5">
          <app-automation-detail-heading
            [rule]="rule"
            [canManage]="canManage()"
            [saving]="saving()"
            (deleteRule)="onDelete($event)" />

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

          <app-automation-rule-summary
            [trigger]="rule.trigger"
            [actions]="rule.actions"
            [statuses]="statuses()" />

          <app-automation-detail-stats [rule]="rule" [runs]="runs()" />

          <section class="flex flex-col gap-3">
            <div class="flex items-center justify-between">
              <h2 class="text-lg font-semibold">
                <span i18n="Heading above the automation run history">
                  Run History
                </span>
              </h2>
              <button app-stroked-button type="button" (click)="load()">
                <span i18n="Button that reloads the run history">Refresh</span>
              </button>
            </div>
            <app-automation-runs-table [runs]="runs()" />
          </section>
        </section>
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
  readonly runs = signal<AutomationRun[]>([]);
  readonly statuses = signal<Status[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal(false);
  readonly canManage = hasPermission(PERMISSONS.automations.manage);

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
      runs: this.service.getRuns(id),
      statuses: this.statusesService.get(),
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: ({ rule, runs, statuses }) => {
          this.rule.set(rule);
          this.runs.set(runs);
          this.statuses.set(statuses);
        },
        error: () => this.error.set(true),
      });
  }

  onDryRun(rule: AutomationRule) {
    const data: AutomationDryRunDialogData = {
      ruleId: rule.id,
      ruleName: rule.name,
    };

    this.dialog.open(AutomationDryRunDialogComponent, { data });
  }

  onToggle(rule: AutomationRule) {
    this.saving.set(true);
    const request = rule.isEnabled
      ? this.service.disable(rule.id)
      : this.service.enable(rule.id);

    request
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.snackbar.open(
            rule.isEnabled
              ? $localize`:Confirmation after switching an automation off:Automation disabled`
              : $localize`:Confirmation after switching an automation on:Automation enabled`
          );
          this.load();
        },
        error: () =>
          this.snackbar.error(
            $localize`:Error after failing to update an automation:Automation could not be updated`
          ),
      });
  }

  onDelete(rule: AutomationRule) {
    this.confirmation
      .open({
        title: $localize`:Title of the confirmation dialog for deleting an automation:Delete Automation`,
        message: `Delete "${rule.name}"? This cannot be undone.`,
        acceptLabel: $localize`:Confirms a destructive action:Delete`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          this.saving.set(true);
          return this.service.delete(rule.id);
        }),
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.snackbar.open(
            $localize`:Confirmation after deleting an automation:Automation deleted`
          );
          void this.router.navigate(['../'], { relativeTo: this.route });
        },
        error: () =>
          this.snackbar.error(
            $localize`:Error after failing to delete an automation:Automation could not be deleted`
          ),
      });
  }

  private ruleId(): number | null {
    const value = Number(this.route.snapshot.paramMap.get('id'));
    return Number.isFinite(value) && value > 0 ? value : null;
  }
}
