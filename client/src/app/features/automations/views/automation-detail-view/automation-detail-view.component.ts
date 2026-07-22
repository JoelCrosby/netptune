import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { ConfirmationService } from '@core/services/confirmation.service';
import { StatusesService } from '@core/services/statuses.service';
import { Status } from '@core/models/status';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import {
  LucideCirclePause,
  LucideCirclePlay,
  LucidePencil,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CardComponent } from '@static/components/card/card.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { EMPTY, finalize, forkJoin, switchMap } from 'rxjs';
import { AutomationDetailHeadingComponent } from '../../components/automation-detail-heading.component';
import { AutomationDetailStatsComponent } from '../../components/automation-detail-stats.component';
import { AutomationRunsTableComponent } from '../../components/automation-runs-table.component';
import { AutomationRuleSummaryComponent } from '../../components/automation-rule-summary.component';
import { AutomationRule, AutomationRun } from '../../models/automation.models';
import { AutomationsService } from '../../services/automations.service';

@Component({
  imports: [
    RouterLink,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    CardComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    AutomationDetailHeadingComponent,
    AutomationDetailStatsComponent,
    AutomationRunsTableComponent,
    AutomationRuleSummaryComponent,
    LucidePencil,
    LucideCirclePause,
    LucideCirclePlay,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header title="Automation">
        <a app-stroked-button [routerLink]="['../']">Back</a>
        @if (canManage() && rule(); as rule) {
          <button
            app-stroked-button
            type="button"
            [disabled]="saving()"
            (click)="onToggle(rule)">
            @if (rule.isEnabled) {
              <svg lucideCirclePause class="h-4 w-4"></svg>
              Disable
            } @else {
              <svg lucideCirclePlay class="h-4 w-4"></svg>
              Enable
            }
          </button>
          <a app-flat-button color="primary" [routerLink]="['edit']">
            <svg lucidePencil class="h-4 w-4"></svg>
            Edit
          </a>
        }
      </app-page-header>

      @if (loading()) {
        <app-page-loading />
      } @else if (error()) {
        <app-card class="min-h-0! p-6! text-center">
          <p class="mb-4 text-sm text-red-500">Failed to load automation.</p>
          <button app-stroked-button type="button" (click)="load()">
            Try Again
          </button>
        </app-card>
      } @else if (rule(); as rule) {
        <section class="flex flex-col gap-5">
          <app-automation-detail-heading
            [rule]="rule"
            [canManage]="canManage()"
            [saving]="saving()"
            (deleteRule)="onDelete($event)" />

          <app-automation-rule-summary
            [trigger]="rule.trigger"
            [actions]="rule.actions"
            [statuses]="statuses()" />

          <app-automation-detail-stats [rule]="rule" [runs]="runs()" />

          <section class="flex flex-col gap-3">
            <div class="flex items-center justify-between">
              <h2 class="text-lg font-semibold">Run History</h2>
              <button app-stroked-button type="button" (click)="load()">
                Refresh
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
  private snackbar = inject(SnackbarService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private store = inject(Store);
  private destroyRef = inject(DestroyRef);

  readonly rule = signal<AutomationRule | null>(null);
  readonly runs = signal<AutomationRun[]>([]);
  readonly statuses = signal<Status[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal(false);
  readonly canManage = this.store.selectSignal(
    selectHasPermission(netptunePermissions.automations.manage)
  );

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
            rule.isEnabled ? 'Automation disabled' : 'Automation enabled'
          );
          this.load();
        },
        error: () => this.snackbar.error('Automation could not be updated'),
      });
  }

  onDelete(rule: AutomationRule) {
    this.confirmation
      .open({
        title: 'Delete Automation',
        message: `Delete "${rule.name}"? This cannot be undone.`,
        acceptLabel: 'Delete',
        cancelLabel: 'Cancel',
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
          this.snackbar.open('Automation deleted');
          void this.router.navigate(['../'], { relativeTo: this.route });
        },
        error: () => this.snackbar.error('Automation could not be deleted'),
      });
  }

  private ruleId(): number | null {
    const value = Number(this.route.snapshot.paramMap.get('id'));
    return Number.isFinite(value) && value > 0 ? value : null;
  }
}
